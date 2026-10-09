const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');
const source=fs.readFileSync(path.resolve(__dirname,'../../EatMyMoviesSite/Views/Movie/Recommender.cshtml'),'utf8');
const script=source.match(/<script>\s*([\s\S]*?)<\/script>/)[1];
function app(fetchImpl=async()=>({ok:true,json:async()=>[]}),windowImpl={scrollTo(){}}) {
    let options;
    vm.runInNewContext(script,{Vue:{createApp(o){options=o;return {mount(){}};}},LoadingSpinner:{},fetch:fetchImpl,window:windowImpl});
    const a={...options.data(),$refs:{},$nextTick:fn=>Promise.resolve().then(fn)};
    for(const [name,fn] of Object.entries(options.methods)) a[name]=fn.bind(a);
    for(const [name,fn] of Object.entries(options.computed)) Object.defineProperty(a,name,{get:fn.bind(a)});
    Object.assign(a,{isLoading:false,selectedFeelings:['FeelGood'],durationPreference:'Any length',openToForeignFilm:true,releaseYearRange:'Anytime',furthestQuestionIndex:3});
    return a;
}
test('Previous/Next stay inside loaded results and do not fetch again',async()=>{
    let requests=0;
    const a=app(async()=>{requests++;return {ok:true,json:async()=>[{title:'First'},{title:'Second'}]};});
    await a.fetchRecommendedMovie();
    a.loadPreviousRecommendation(); assert.equal(a.recommendedMovie.title,'First');
    a.loadNextRecommendation(); a.loadNextRecommendation(); assert.equal(a.recommendedMovie.title,'Second');
    assert.equal(a.hasNextRecommendation,false);
    a.loadPreviousRecommendation(); assert.equal(a.recommendedMovie.title,'First');
    assert.equal(a.hasPreviousRecommendation,false); assert.equal(requests,1);
    assert.match(a.resultAnnouncement,/First, recommendation 1 of 2/);
});
test('Editing preserves every answer, allows completed steps, and replaces results on resubmit',async()=>{
    let requests=0;
    const a=app(async()=>({ok:true,json:async()=>[{title:++requests===1?'Old':'New'}]}));
    await a.fetchRecommendedMovie(); a.editAnswers();
    assert.equal(a.currentQuestionIndex,0); assert.equal(a.showRecommendationBox,false);
    assert.equal(a.selectedFeelings[0],'FeelGood'); assert.equal(a.durationPreference,'Any length');
    assert.equal(a.openToForeignFilm,true); assert.equal(a.releaseYearRange,'Anytime');
    a.goToQuestion(3); assert.equal(a.currentQuestionIndex,3);
    await a.fetchRecommendedMovie(); assert.equal(a.currentRecommendationIndex,0); assert.equal(a.recommendedMovie.title,'New');
});
test('No results keeps answers editable and a failed request keeps a retryable state',async()=>{
    const empty=app(); await empty.fetchRecommendedMovie();
    assert.equal(empty.recommendedMovie,null); assert.equal(empty.showRecommendationBox,true);
    empty.editAnswers(); assert.equal(empty.selectedFeelings[0],'FeelGood');
    const failure=app(async()=>({ok:false})); await failure.fetchRecommendedMovie();
    assert.match(failure.loadError,/unavailable/); assert.equal(failure.isLoading,false);
    assert.equal(failure.allQuestionsAnswered,false); assert.equal(failure.selectedFeelings[0],'FeelGood');
});
test('Removing an earlier answer cannot send an incomplete recommendation request',async()=>{
    let requests=0; const a=app(async()=>{requests++;throw new Error('unexpected');});
    a.selectedFeelings=[]; a.currentQuestionIndex=3; await a.fetchRecommendedMovie();
    assert.equal(requests,0); assert.equal(a.currentQuestionIndex,0);
});
test('Focus waits for a mounted destination and works after the transition hook retries',async()=>{
    const a=app(); a.requestViewFocus(); await Promise.resolve();
    assert.equal(a.pendingFocus,true);
    let focused=false,scrolled=false;
    a.$refs.questionHeading={focus(){focused=true;},scrollIntoView(){scrolled=true;}};
    a.focusCurrentView(); assert.equal(focused,true); assert.equal(scrolled,true); assert.equal(a.pendingFocus,false);
});

test('Results open at the page top after mounting without scrolling past the mobile poster',async()=>{
    for (const movies of [[{title:'First'}],[]]) {
        let scrollOptions,focusOptions;
        const a=app(async()=>({ok:true,json:async()=>movies}),{scrollTo(options){scrollOptions=options;}});
        await a.fetchRecommendedMovie();
        assert.equal(a.pendingFocus,true);
        assert.equal(scrollOptions,undefined);
        a.$refs[movies.length?'resultHeading':'emptyHeading']={
            focus(options){focusOptions=options;},
            scrollIntoView(){assert.fail('Result headings must not scroll past the poster');}
        };
        a.focusCurrentView();
        assert.equal(focusOptions.preventScroll,true);
        assert.equal(scrollOptions.top,0);
        assert.equal(scrollOptions.behavior,'instant');
        assert.equal(a.pendingFocus,false);
    }
});
