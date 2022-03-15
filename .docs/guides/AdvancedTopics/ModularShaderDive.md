---
uid: adv-GeneratorDive
title: Modular Shader Generator Deep Dive
---

# Modular Shader Generator Deep Dive

The modular shader generator is fairly simple to use, you just call the [GenerateShader](xref:VRLabs.ModularShaderSystem.ShaderGenerator.GenerateShader(System.String,VRLabs.ModularShaderSystem.ModularShader,System.Boolean)) method, pass in the destination folder path, the modular shader, and you're done.

But it may be useful to know what happens underneath and talk about the generation steps.

First of all, the [GenerateShader](xref:VRLabs.ModularShaderSystem.ShaderGenerator.GenerateShader(System.String,VRLabs.ModularShaderSystem.ModularShader,System.Boolean)) is a wrapper that sets up one or more [ShaderContext](xref:VRLabs.ModularShaderSystem.ShaderGenerator.ShaderContext) objects, and these objects are what actually generate the shader.
This separation is done so that the generator is unified in all use cases, for example in the library it's used for both generating the shader and generating optimised shaders.

## GenerateShader method

In the case of the [GenerateShader](xref:VRLabs.ModularShaderSystem.ShaderGenerator.GenerateShader(System.String,VRLabs.ModularShaderSystem.ModularShader,System.Boolean)) method, it first retrieves and reloads all the used template assets,
then it evaluates all the possible variants combinations the modular shader has (as a reminder, variants are those templates that to be able to have the module toggled on and off, need to have the code actually removed, ending up with multiple shader files), 
after that it generates the PropertyBlock string, which contains all properties declared in all modules (if you have the shader setup to use templates for properties, only the ones in the dedicated template in the modular shader asset is included here, the rest will be handled later with the other templates).

The PropertyBlock string is generated outside of the shader context here cause they will all share the same properties, so you only need to generate it once.

> [!NOTE]
> in an ideal world reloading all template assets needed should not be done since you should be able to just take the reference from the modular shader and shader module assets, but in some specific situations (specifically when you first import unitypackages containing a modular shader, up until editor restart) that returns empty assets instead of our templates, so we just reload them for the generation process


Now for each variant combination a ShaderContext is generated, by passing all its relevant informations.

Now that we have a list of contexts to run, we will just process them all in parallel, since everything that a context does is manipulating strings, we aren't limited to do it inside the main thread. This speeds up a lot the generation of shaders that have a lot of variant combinations. 

After all the contexts are done, we tell unity that we are starting to edit assets, write down files with the result of each context, and then tell unity we finished editing, so that it can import the newly generated shaders.

To finish it off, we load all the newly generated shaders and add a reference to them in the modular shader asset, this way the modular shader always contains a reference to the last shaders that were generated with it.

## The ShaderContext 

As we previously said, a [ShaderContext](xref:VRLabs.ModularShaderSystem.ShaderGenerator.ShaderContext) takes care of generating a single shader file using the informations it has been given by calling its [GenerateShader](xref:VRLabs.ModularShaderSystem.ShaderGenerator.GenerateShader(System.String,VRLabs.ModularShaderSystem.ModularShader,System.Boolean)) method.

First it generates the name for both the file and the shader path in the material's shader selector, after that it generates the property section of the shader by using the provided property block or by generating its own if it's empty (in this case it makes the assumption that it's doing it for optimised shaders and does not include module's Enable properties).

### Adding templates

Then it's time to generate the SubShader block by applying the templates. They get listed from modules and then reordered by queue (this should keep the templates with the same queue ordered by position of their relative modules).

Then for each template there's a check to see if the template is a toggleable template that doesn't need a variant, in which case the template code gets enclosed by an `if` check (in optimised shaders this would never happen since the module would not be even in the list of used modules for the context generation).

After that it checks for the presence of any `internal keyword`, and each found one gets replaced with a runtime generated one that is dependent on the module id and original internal keyword. This is to assure that internal keywords that are in multiple modules don't actually get used by different modules from the one it was intended for.

> [!NOTE]
> the instanced internal keywords are stored in a dictionary where the key is a combination of module id and original internal keyword, so that it can be easily retrieved when we search an internal keyword for a specific template in a module.

Now it's time to add the template to the main code by finding the indexes of the selected keywords (or instanced internal keywords), and inserting the template string (with the mentioned modifications) in each of these indexes, from the last one to the first one.

> [!NOTE]
> We go from last to first index because if we went the other way around after the first index is used the others would not be valid, since the keywords positions have shifted indexes by the amount of characters equivalent to the lenght of the inserted template.
> 
> We could have just taken that into account and also shifter our indexes after each iteration, but it's just simpler to start from the last and go backwards.

Once all templates have been dealt with, all the instanced internal keywords get removed, since they're not going to be used anymore.

### Adding functions

Now it's time to add all the functions declared in modules, which is a bit more involved process since there are multiple things to keep track of.

Fist we list all the functions that have to be added, then we start by adding all the variables in their respective keywords.
This is done by looping each function, looking for which variables it declares, check which keywords they're supposed to go in, and then adding them to the respective list (these lists are contained in a dictionary where the key is the keyword they should go in).

iterating all functions for their variables, for each list a string with the variable declarations is generated (duplicates of variables get removed here), and then each string is added to the shader code with the same logic templates did (minus the internal keywords that are not here anymore).

Now that variables are dealt with, it's time for the functions themselves. Their order depends first on where they declare to go (keyword or other functions), then by their queue.
Due to that, functions that go to keywords are the first to be looked at, since they are the root of the call chain. Therefore we look at all keywords that are being used by functions, and for each keyword we list all functions that go there, and order them by queue. Just like templates, we check if the function needs to be able to be disabled and in that case we enclose the call with an `if` check, and then append the string to the call sequence. 

Before cycling to the next function in the list, we need to check if the function has other functions that declare to be appended right after it. This is pretty much the same process done with these top level functions, but using the function name instead of a keyword, so the entire process can be repeated recursively. And after that the next function in the list is evaluated and the process repeats.

At the end of the call sequence evaluation of each keyword, the entire call sequence string gets appended to each keywords, with the same logic already used for templates and variables.

During the evaluation process functions also get reordered into a list where the order is dependent on when a function has been used by the sequence, and this is used now to write down the actual function code implementation stored in template assets.
For each function in the reordered list its code template string is taken and added to the relative keyword (in case of no keywords declared, the default `DEFAULT_CODE` keyword is used).

> [!NOTE]
> the code strings are stored in a dictionary of StringBuilders first, and then added to the main code by keywords later on, so that the final insertion to the keywords is done only once per keyword.

During this process is also made sure that a code implementation template is not added more than once per keyword, to avoid code duplication that would make the shader fail to compile.

### Final steps

Now that functions have been taken of, there's some last things to take care of.
First is to add the defined `CustomEditor` so that is used the inspector that has been defined in the modular shader asset, then a custom `PostGeneration` action is called.
This action can be passed to the shader context to have custom code run at this step, this could be useful in case someone needs to do some edits to the shader code before it is finalized.

> [!TIP]
> You can still look for keywords at this stage.

After that all the keywords used up to now get removed from the final shader code, since they are not needed anymore.

And everything ends with a final code cleanup where the shader code gets indented correctly (for the most part) and line terminators get normalized.

