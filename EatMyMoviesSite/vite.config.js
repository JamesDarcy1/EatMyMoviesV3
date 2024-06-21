// vite.config.js
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
    plugins: [vue()],
    base: './', // Ensure correct base path for your project
    server: {
        port: 5174, // Adjust as necessary, make sure it doesn't conflict with ASP.NET port
    },
});
