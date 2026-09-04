# 🧹 Windows Debloater — Demo Version

> **Demo / Prototype:** This project was developed during an AI Hackathon as a proof of concept. It demonstrates the main functionality of a Windows debloating and cleanup tool and is not intended to be a production-ready system utility.

A lightweight Windows desktop application designed to help users remove selected pre-installed applications and clean temporary system files through a simple graphical interface.

The project was developed collaboratively during an **AI Hackathon**, with AI-assisted development used to accelerate prototyping and implementation.

## ✨ Features

### Application Debloating

Users can select applications from a predefined list and attempt to remove them through the application.

Supported applications in this demo include:

* Microsoft Copilot
* OneDrive
* Windows Calculator
* Bing Weather
* Microsoft News

### 🧹 System Cleanup

The demo also provides several cleanup options:

* Windows temporary files
* Edge/Chrome browser cache
* Recent files cache
* Windows thumbnail cache

The application displays the cleanup results, including deleted files and freed disk space.

### ⚙️ Automated Uninstallation

The application attempts to uninstall supported applications using **Windows Package Manager (`winget`)**. When applicable, it can fall back to **PowerShell/Appx package removal**.

Command output and errors are displayed in the application interface.

## 🛠️ Technologies

* **C#**
* **.NET 8**
* **Avalonia UI**
* **XAML**
* **PowerShell**
* **Windows Package Manager (winget)**
* **Windows Appx package management**

## 🚀 Running the Demo

### Prerequisites

* Windows
* .NET 8 SDK
* Windows Package Manager (`winget`)

Clone the repository:

```bash
git clone https://github.com/Ksenija008/DebloaterApp.git
cd DebloaterApp
```

Run the application:

```bash
dotnet run
```

## ⚠️ Demo Limitations

This is a **hackathon demo/prototype**, not a production-ready Windows maintenance tool.

The application currently uses a predefined list of applications and cleanup locations. Some uninstall or cleanup operations may depend on the Windows version, installed packages, user permissions, or system configuration.

Users should review selected operations before running them, as removing applications or system files may affect Windows functionality.

## 🎓 Project Background

This project was created as a **demo/proof of concept during an AI Hackathon**.

The goal was to rapidly develop a functional desktop application that demonstrates how Windows application management and system cleanup could be combined into a single user interface.

The project provided practical experience with:

* C# and .NET desktop development
* GUI development with Avalonia UI
* Windows system automation
* PowerShell integration
* Process execution and error handling
* Rapid prototyping
* Collaborative development
* AI-assisted development



