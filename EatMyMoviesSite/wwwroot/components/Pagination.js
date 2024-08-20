// components/pagination.js
export default {
    props: {
        currentPage: Number,
        totalPages: Number,
        actionName: String
    },
    template: `
        <nav class="pagination is-centered mt-5" role="navigation" aria-label="pagination">
            <ul class="pagination-list">
                <li v-if="currentPage > 1">
                    <a class="pagination-link" @click.prevent="goToPage(currentPage - 1)">Previous</a>
                </li>
                <li v-for="page in totalPages" :key="page">
                    <a :class="['pagination-link', { 'is-current': page === currentPage }]" @click.prevent="goToPage(page)">
                        {{ page }}
                    </a>
                </li>
                <li v-if="currentPage < totalPages">
                    <a class="pagination-link" @click.prevent="goToPage(currentPage + 1)">Next</a>
                </li>
            </ul>
        </nav>
    `,
    methods: {
        goToPage(page) {
            if (page >= 1 && page <= this.totalPages) {
                window.location.href = `/List/${this.actionName}?page=${page}`;
            }
        }
    }
};
