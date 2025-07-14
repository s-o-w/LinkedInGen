---
applyTo: '**'
---
# Project Description for the LinkedInGen solution
This application will do 2 things:
- prompt the user for a directory containing CSV files that have LinkedIn profile information. let them select the files that they want the program to analyse, and then ask them where to save the prompts that the LLM will generate. Save those prompts as skprompt.txt plugins suitable for use in semantic kernel.
- prompt the user for a post topic and some details, and then use the plugins found to generate a new post for a user in their voice (via the use of the semantic kernel plugins) and then ask if the user would like to revise the content, via a loop.

## Project technical details
- SIMPLER IS BETTER!!! Always consider that we want to create the simplest possible solution that will work, don't add extra code or classes or anything that aren't explicitiy neccassary!!!
- SERIOUSLY, DON'T OVER-COMPLICATE THINGS - SIMPLEST POSSIBLE SOLUTION!!!!
- This is a dotnet console application, use proper dotnet c# coding standards and practices. Ensure that the code is clean, well-structured, and follows best practices for maintainability and readability.
- Use appropriate naming conventions for classes, methods, and variables.
- Ensure that the code is modular and follows the single responsibility principle.
- Use dependency injection where appropriate to enhance testability and flexibility.
- Ensure that error handling is implemented where necessary, and consider using logging for debugging and monitoring purposes.