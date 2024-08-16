# React Code Generator Utility

This utility is designed to generate the necessary React files based on an OpenAPI specification, streamlining the creation of types, API methods, Redux slices, and sagas.

## Prerequisites

Before using this utility, ensure you have the following:

- **Windows** Operating System
- The OpenAPI (Swagger) specification file (`swagger.json`)

## Usage Instructions

### Step 1: Download and Prepare

1. **Download the Utility**  
   Locate the `app` folder provided with this project. This folder contains the pre-compiled executable file named `ReactCodeGenerator.exe`.

2. **Place the Swagger Specification File**  
   Ensure your `swagger.json` file is ready, as you will need to provide its path when running the utility.

### Step 2: Running the Utility

1. **Open Command Prompt**  
   Press `Win + R`, type `cmd`, and press Enter to open the Command Prompt.

2. **Navigate to the App Directory**  
   Use the `cd` command to navigate to the folder containing `ReactCodeGenerator.exe`. For example:
   ```bash
   cd path\to\your\app\folder
   ```

3. **Run the Utility**  
   Execute the following command, replacing `path\to\swagger.json` with the path to your OpenAPI specification file and `ComponentName` with the desired component name:
   ```bash
   ReactCodeGenerator.exe "path\to\swagger.json" "ComponentName"
   ```
   **Example**:
   ```bash
   ReactCodeGenerator.exe "C:\Users\YourName\Documents\swagger.json" "IPConfiguration"
   ```

### Step 3: Generated Files

After running the utility, the generated files will be placed in the appropriate folders according to the structure required for your React project. These files include:

- `ComponentName.types.ts`
- `ComponentName.api.ts`
- `ComponentName.slice.ts`
- `ComponentName.saga.ts`
- Container and content files for the component

### Step 4: Integrating with Your React Project

1. **Copy the Generated Files**  
   Copy the generated files from the output folder into the appropriate directories within your React project.

2. **Update Your Project**  
   Add the necessary imports in your Redux store and sagas to integrate the generated slices and sagas.

3. **Build and Run**  
   After integrating the files, build and run your React project as usual.

### Troubleshooting

If you encounter any issues while using the utility or integrating the generated files, ensure that your `swagger.json` is correctly formatted and that the paths provided are accurate.

---

This formatted `README.md` should be easy to read and follow, ensuring that users can run the utility smoothly without compiling the code themselves.
