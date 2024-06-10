<template>
    <div class="recommender-container">
        <h1>Recommender</h1>
        <div v-if="currentQuestionIndex < maxQuestions">
            <div class="question-area">
                <p>{{ currentQuestion }}</p>
                <button class="button is-primary" @click="handleYes">Yes</button>
                <button class="button" @click="handleNo">No</button>
            </div>
        </div>
        <div v-else>
            <p>Then I'm not recommending any more films.</p>
        </div>
        <div v-if="recommendedMovie" class="movie-area">
            <h2>{{ recommendedMovie.title }}</h2>
            <p>{{ recommendedMovie.synopsis }}</p>
            <p>Rating: {{ recommendedMovie.imdbRating }}</p>
        </div>
    </div>
</template>

<script>
    import { ref, computed } from 'vue';

    export default {
        setup() {
            const genres = ref([]);
            const currentQuestionIndex = ref(0);
            const maxQuestions = ref(5);
            const recommendedMovie = ref(null);

            // Computed property to get the current question
            const currentQuestion = computed(() => {
                if (genres.value.length > 0 && currentQuestionIndex.value < genres.value.length) {
                    return `Are you feeling some ${genres.value[currentQuestionIndex.value]}?`;
                }
                return '';
            });

            // Handle the 'Yes' button click
            const handleYes = () => {
                const genre = genres.value[currentQuestionIndex.value];
                fetch(`/Movie/GetRecommendation?genre=${genre}`)
                    .then(response => response.json())
                    .then(data => {
                        recommendedMovie.value = data;
                    });
            };

            // Handle the 'No' button click
            const handleNo = () => {
                if (currentQuestionIndex.value < maxQuestions.value - 1) {
                    currentQuestionIndex.value++;
                } else {
                    currentQuestionIndex.value = maxQuestions.value;
                }
            };

            // Fetch genres from the server on component mount
            fetch('/Movie/GetGenres')
                .then(response => response.json())
                .then(data => {
                    genres.value = data;
                });

            return {
                genres,
                currentQuestionIndex,
                maxQuestions,
                recommendedMovie,
                currentQuestion,
                handleYes,
                handleNo
            };
        }
    };
</script>
