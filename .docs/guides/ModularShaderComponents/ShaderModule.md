---
uid: str-ShaderModule
title: Shader Module
---

# Shader Modules

A module is a component that can be added and removed from a modular shader. Like the modular shader asset it has a `Base Information` and `Settings` sections, but unlike in the modular shader, you actually need to set at least some of the `Base Information` settings, since they're used for checking module compatibility inside a modular shader.

You can create a shader module asset by selecting the menu `Assets > Create > Shader > VRLabs > Modular Shader > Shader Module`.

## Basic Informations

### Id:

Id of the module. Like the Modular Shader one, but it is used to check duplicates, incompatible modules and required modules inside a modular shader.

You really should stick to a specific naming convention here.

### Name:

User friendly name for the shader module.

### Author:

Author of the shader module.

### Version:

Version of the shader module.

### Description:

A short description of the shader module.

### Module Dependencies:

List of dependencies this module has, you should have the id of the modules this module needs in order to work.

### Incompatible With:

List of modules that are incompatible with this module, works the same as module dependencies but for incompatibility instead.

## Settings

### Enabled:

This is a special int shader property that will define if the module is enabled or not. When this area is filled the module will have the ability to be turned on and off entirely.

This works by having the generated shader containing conditional statements for function calls and templates of this module, unless the template needs a `variant`, in that case the system just generates multiple versions of the shader based on the combinations of the variants turned on and off.

Eventual optimized shaders will have disabled modules completely removed from the shader code, making it a good way to optimise out unused features once the material settings are of your liking.

> [!NOTE]
> The library has an API to generate optimised shaders, but that's not used anywhere by default, so if you need the feature you need to create your own editor tool to generate optimised shaders.

### Properties:

Acts just like the properties in the modular shader asset.

### Templates:

A list of templates the module has.

In here you have the possibility to set the template assets, and select which keywords this template code is going to placed into.

> [!WARNING]
> The keyword has to omit the `#K#`, so put the template inside the keyword `#K#MY_KEYWORD` you should write `MY_KEYWORD`.
> 
> This behavior with keywords is the same everywhere except where specifically noted.

The queue value also determines when this placement of the code in the shader, which can drastically change the result. lower numbers means the operation is done before templates with higher numbers.

The `Generate variant` toggle defines if this template does not support being inside an if statement due to it being placed in areas of the shader where that would be a syntax error (basically everywhere except inside some functions, like the vertex or fragment functions).
When it's checked, the system will generate multiple versions of the shader, one with this code in there, and one without. Multiple variants will cause the generator to generate an exponential amount of shaders, due to the need to check for all possible cases.

### Functions:

The more complicated part of the module.

It's a bigger abstraction compared to templates, and needs more data to be filled in order to work. But this also gives the system more infos to play with, which comes really useful when using the debugging tools offered by the Modular Shader System.

Each function is defined by:
- A name.
- A template containing the function definition.
- An `Append After` string.
- A `Queue` value.
- A list of variables used by the function.
- A list of keywords for variables.
- A list of keywords for the function code.

#### Name

It's the name of the function that has to be called.

#### Template

It's the template containing the code of the function, as well as other dependent code that the function needs.

It should have a `void` function with no parameters called the same way of the `Name` defined before, also it should avoid defining any variable, since those should be given to the variables list.

This function will be called in the point defined by the `Append After`.

#### Append After

This property defines where this function call is placed.

2 types of values are valid here:
- The name of another declared function.
- A keyword that is placed inside a function.

When using another declared function, you just type the name of said function, when using a keyword instead type the full keyword name, with the `#K#` included since the system has to know you want to place it after a keyword and not after a function call.

#### Queue

This works similarly to how templates Queue work, where low numbers will be written in the shader before higher ones.

The main difference is that this queue value is valid only in the context of the same `Append After` value. 
This is due to the fact that each function call under the same `Append After` will be placed right after said function, and before whatever function was about to be placed after that one. 

You can try to play with it and check the [Function Timeline](xref:dbg-FunctionTimeline) in the [Modular Shader Debugger](xref:dbg-ModularShaderDebugger) to see the order of the functions calls.

#### Variables

In this list you define all the variables that are going to be used by this function and that are not available by default from some template.

You should declare a variable here even if another function placed above already defines it. the system will only create the variable once upon generation, but this way has the knowledge that both functions use it.

The variable values will depend if other functions before this one used it and set it, or it has never been used. So this is the main way you can pass results from a function to another.

> [!NOTE]
> Remember to write down variables that derive from a shader property as well, because by default the system will not assume that all properties are used inside the shader.


> [!TIP]
> You should keep in mind that the variable may have not been used by any function until this function uses it. If you need this variable to have some value that is defined by another function in another specific module, you should make sure this function is called after, and also to set this module is dependent on.
> 
> If you just need to have the variable be defined and just act based on its value, then just be sure that the function can handle a default value on said variable.

#### Variable Keywords

This is a list of keywords where all the variables used by this function will be declared to, it not already there. 
Ideally this keyword is placed in an area before all functions implementations, so that the functions can access those variables.
If empty the variables will automatically go after the default `DEFAULT_VARIABLES` keyword (the keyword has to be defined somewhere in the templates, or these variables will not be defined anywhere).

#### Function Keywords

Same as the variable keywords, but for placing the code inside the templates given to the function.
If empty the template code will automatically go after the default `DEFAULT_CODE` keyword (the keyword has to be defined somewhere in the templates, or the code will not be placed anywhere).

