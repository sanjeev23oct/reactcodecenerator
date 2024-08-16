React Code Generator Utility
This utility generates the necessary React files based on an OpenAPI specification. The generated files include types, API methods, Redux slices, and sagas.

Prerequisites
Before you begin, ensure that you have the following installed on your system:

Windows Operating System
The OpenAPI (Swagger) specification file you want to use (swagger.json).
Usage Instructions
Step 1: Download and Prepare
Download the Utility:

Locate the app folder provided with this project. The folder contains a pre-compiled executable file named ReactCodeGenerator.exe.
Place the Swagger Specification File:

Ensure you have your swagger.json file ready, as you will need to provide its path to the utility.
Step 2: Running the Utility
Open Command Prompt:

Press Win + R, type cmd, and press Enter to open the Command Prompt.
Navigate to the App Directory:

Use the cd command to navigate to the folder where the ReactCodeGenerator.exe is located. For example:
bash
Copy code
cd path\to\your\app\folder
Run the Utility:

Run the following command, replacing path\to\swagger.json with the path to your OpenAPI specification file and ComponentName with the desired component name:
bash
Copy code
ReactCodeGenerator.exe "path\to\swagger.json" "ComponentName"
Example:
bash
Copy code
ReactCodeGenerator.exe "C:\Users\YourName\Documents\swagger.json" "IPConfiguration"
Step 3: Generated Files
After running the utility, the generated files will be placed in the appropriate folders as per the structure required for your React project. These include:

ComponentName.types.ts
ComponentName.api.ts
ComponentName.slice.ts
ComponentName.saga.ts
Container and content files for the component.
Step 4: Integrating with Your React Project
Copy the Generated Files:

Copy the generated files from the output folder into your React project's directory structure.
Update Your Project:

Add the necessary imports in your Redux store and sagas to incorporate the generated slices and sagas.
Build and Run:

After integrating the files, build and run your React project as usual.
Troubleshooting
If you encounter any issues while using the utility or integrating the generated files, ensure that your swagger.json is correctly formatted and that the paths provided are accurate.

This README should help users run the utility directly without needing to compile the code themselves.
