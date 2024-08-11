# Now RPA Code Analyzer
## Description
Now RPA Code Analyzer is a powerful command-line tool designed to analyze `.iBot` files used in Now RPA (Robotic Process Automation) development. It performs comprehensive checks on diagnostics, framework compliance, and code quality, providing valuable insights to improve your RPA implementations.
- Interactive Input
![image](https://github.com/user-attachments/assets/a46006fe-9ea9-44de-afee-6e63198dc3d5)
- Summary
![image](https://github.com/user-attachments/assets/4656d004-77ab-495c-8c0d-1085b09b55c8)
- Interactive Menu
![image](https://github.com/user-attachments/assets/0207a135-419f-47ce-afb6-067f866decca)
- Detailed Report
![image](https://github.com/user-attachments/assets/b94f8fd2-6acd-4c0c-a2e4-2929539dca13)
- Bot Strcuture
![image](https://github.com/user-attachments/assets/c5e8782c-d586-406f-8867-361ef28ce053)


## Features
Parse and analyze iBot XAML files
Generate detailed CSV reports with analysis results
Supports both interactive terminal UI and command-line execution
Multiple rule sets: Diagnostics, Framework, and Code Quality
Easy-to-use command-line interface and interactive menu system


## Usage
### Configuration
The rule sets can be configured by modifying the JSON configuration files located in the `Config` directory:

`Diagnostics.json`
`Framework.json`
`CodeQuality.json`
### Interactive Mode
Run the application directly without any arguments to start in interactive mode:
Follow the on-screen prompts to analyze your iBot files.

### Command-Line Mode
To use the command-line mode, run the application with the following arguments:
```
CodeAnalyzer.exe --input <path-to-ibot-file> [--output <path-to-output-csv>]
```
Options:

`--input` or `-i`: (Required) The path to the .iBot file you want to analyze.
`--output` or `-o`: (Optional) The path where you want to save the CSV report. If not specified, it will be saved in the same directory as the input file.

Example:
```
CodeAnalyzer.exe --input C:\MyBot.iBot --output C:\Analysis\MyBotAnalysis.csv
```


## Output
The tool generates a CSV file containing the results of various rule checks. Each row in the CSV represents a single rule check result, including information such as:

Rule category
Rule name
Status (Pass/Fail/Warning)
Source (location in the iBot file)
Comments or additional details

### Note on Emoji Display
In case emojis are not visible in the Code Analyzer interface:

- Use a terminal that supports emoji display.
- Consider installing Windows Terminal app from the Microsoft Store for better emoji support.
- Enable system-wide UTF-8 support:
  - Open Run dialog (Windows key + R)
  - Enter `intl.cpl` and press Enter
  - Navigate to the Administrative tab
  - Click "Change system locale"
  - Check "Beta: Use Unicode UTF-8 for worldwide language support"
  - Click OK and restart your system when prompted
    ![image](https://github.com/user-attachments/assets/fcbe3eee-7065-4026-a5a8-1bf3bbb2c93e)


These steps ensure proper display of emojis and Unicode characters, enhancing the visual experience of the Code Analyzer tool.
## Building Project
Build the project:
```
dotnet build
```
