const LoadingSpinner = {
    template: `
            <img class="img" src="../red-spinner.gif" alt="loading..." />
    `,
    name: "LoadingSpinner",
    mounted() {
        const style = document.createElement('style');
        style.textContent = `
            .img {
              //position: relative;
              //margin: auto;
              //margin-top: 20%;
              //height: 100px;
              //width: 100px;
            }

        `;
        this.$el.appendChild(style);
    }
};

//// Register the component globally
//Vue.component('loading-spinner', LoadingSpinner);
