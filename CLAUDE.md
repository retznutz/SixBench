## What this project is about

This project is a application server designed to serve the hdmi output of a physical Roku through a web browser.  It also allows control of the Roku via a web interface.


## Technical Architecture Overview

- **.NET Version**: Target .NET 10.0 or later for improved performance and long-term support.
- **API Layer**: Implement RESTful APIs using ASP.NET Core Web API. Use attribute routing for defining endpoints and ensure proper versioning of APIs.
- **Business Logic**: Encapsulate business rules and logic in separate service classes. Use Dependency Injection to manage service lifetimes and dependencies.
- **Logging & Monitoring**: Integrate Serilog for structured logging. Set up Application Insights for monitoring application performance and tracking errors.
- **Configuration Management**: Use appsettings.json for configuration settings. Leverage environment-specific configuration files (e.g., appsettings.Development.json) for different deployment environments.
- **Environment**: The application should be cross-platform, running on Windows, Linux, and macOS using .NET Core runtime. 
- **Pathing**: Use forward slashes (/) for all file paths in the codebase to ensure consistency across different operating systems. Normalize paths in the code to handle both Windows and Unix-style paths correctly. 
- **Documentation**: Maintain API documentation using Swagger/OpenAPI. Ensure that all public methods and classes are well-documented with XML comments.  
- **Frontend**: Always use NPM for frontend dependencies. Use Nuxt 4 for the frontend framework. Ensure the frontend is decoupled from the backend, communicating via API calls.  Use TypeScript for frontend development to enhance type safety and maintainability.  Also create stores for api calls and state management.  Use Tailwind CSS for styling to maintain a consistent design system across the application.  Ensure the frontend is responsive and works well on both desktop and mobile devices.  Use PrimeVue for UI components to speed up development and maintain a polished user interface.  Use Vue Router for client-side routing to create a seamless single-page application experience.  Use Pinia for state management to handle complex state interactions in the frontend.  Implement lazy loading of components and routes to improve performance and reduce initial load times.  Ensure proper error handling and display user-friendly error messages in the frontend when API calls fail or return errors.  Types should be defined for all API responses and broken into seperate files and used throughout the frontend to ensure type safety and reduce bugs.  Use ESLint and Prettier for code linting and formatting to maintain a consistent code style across the frontend codebase.  

## Co-Development Guidelines

- **Version Control**: Use Git for version control. Follow the Gitflow workflow for branching and merging.
- **Pair Programming**: I'll be working on this with you .  If I change somethings, don't change it back without discussing it with me first.  If you have questions, ask them.
- **Database Table Names**: Use singular nouns for table names (e.g., "User" instead of "Users").
- **Debug Lines**: If you try more than 3 times to fix an issue, and ypou are still stuck, add debug lines to the code to help identify the issue.  Then share those lines with me and we can work through it together.