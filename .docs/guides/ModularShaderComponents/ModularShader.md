---
uid: str-ModularShader
title: Modular Shader
---

# Modular Shader

This Asset contains all the basic information about the shader, divided in the following categories:
- **Base Informations:** this part is mainly informational, but could be used by systems made to work with the modular shader system.
- **Settings:** data here will be used to generate the shader.

You can create a modular shader asset by selecting the menu `Assets > Create > Shader > VRLabs > Modular Shader > Modular Shader`.

## Basic Informations

Info about the shader, not really used by the modular shader system, but available for third party implementations.

### Id: 

Identifier for the shader, should stay unique. Normally it should have a namespace like structure like `Author.Name.Subname`, but anything goes.

### Name:

User friendly name for the modular shader.

### Author:

Author of the modular shader.

### Version: 

Version of the modular shader.

### Description:

A short description of the modular shader.

## Settings

Settings of the shader, which will change how the shader gets generated.

### Shader template:

The shader template is just shader code that will go inside the main `Subshader` block. This will be the base of the shader and where tha main hooks will be.
These hooks are called `Keywords` (not to be confused with shader keywords) and they're always defined by starting the line with `#K#` (the entire line should be just the keyword).
> [!NOTE]
> The modular shader system currently only supports one Subshader block.

> [!NOTE]
> We talk more in specific about Keywords [in this page](xref:str-Keywords).

### Shader Path:

It will be shader path of this modular shader when searching in the material settings.

### Custom Editor:

Custom editor the shader will use.
> [!NOTE]
> Being a modular shader, you may end up having it changing the properties available, so you should probably account for that in your custom inspector

### Properties:

The shader properties that you're going to have. These properties wil always be included in the generated shader, but they may not be the only properties available, since each module can also have properties.

### Properties from templates:

If for some reason you want to have more control on how the properties are declared, you check this toggle and you will have available a keyword called `SHADER_PROPERTIES` from which you can point templates to in modules. You also can set a base template for setting properties that are always going to be available, just like you would if you set them in the properties section.

> [!WARNING]
> It is preferable to have properties in the properties list compared to have them inside a template, but you're free to take the approach you want.

### Base Modules list:

One of the most important part of the settings, the `Base modules` list, contains the modules that are going to be used by the shader. These modules are what will compose the final generated shader.

> [!NOTE]
> The `basic modules` is not the full list of modules, there's also another list of modules that are not listed by the inspector, but that's meant to be used by third party systems that want to automatically manage modules.
