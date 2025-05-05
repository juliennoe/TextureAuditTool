# 🚀 Texture Audit Tool

> **Prerequisites**  
> - Unity 2023.1 or later  
> - Addressables package installed (`Window → Package Manager → Addressables`)

A Unity Editor extension to **audit** all your project’s textures and sprites.

---

## 📋 Table of Contents

1. [✨ Features](#-features)  
2. [📦 Installation](#-installation)  
3. [🛠️ Usage](#️-usage)  
4. [⚙️ Configuration](#️-configuration)  
5. [🤝 Contributing](#-contributing)  
6. [📝 License](#-license)  

---

## ✨ Features

- 🖼️ **MipMap** status detection  
- 📐 **Power-of-two** and **multiple-of-four** resolution checks  
- 🗂️ **Sprite Atlas** membership indicator  
- 🎯 **Addressable** asset flag  
- 🔍 **Resolution**, **format**, **file size** and **memory** usage display  
- 🔎 Search and sort assets by any column  
- 📁 Click asset name to **ping** it in the Project window  
- 🔄 Persistent data until next scan  

---

## 📦 Installation

1. In Unity, open **Window → Package Manager**  
2. Ensure **Addressables** is installed  
3. Click **+** → **Add package from disk...**  
4. Select this package’s **package.json**  
5. Open **Window → Tools → Texture Audit**  

---

## 🛠️ Usage

1. Go to **Window → Tools → Texture Audit**  
2. Click **Scan** to gather texture data  
3. Use the 🔍 search bar to filter by name  
4. Select sort criteria from the dropdown  
5. Toggle column visibility via the checkboxes  
6. Click an asset name to locate it in the Project window  

---

## ⚙️ Configuration

You can customize which columns are shown in the tool’s script:

```csharp
// In TextureAuditTool.cs
private bool showMipmap         = true;
private bool showPowerOfTwo     = true;
private bool showMultipleOfFour = true;
private bool showInSpriteAtlas  = true;
private bool showAddressable    = true;
private bool showResolution     = true;
private bool showFormat         = true;
private bool showFileSize       = true;
private bool showMemory         = true;
private bool showPath           = true;
