---
uid: GettingStarted
title: Getting started
---

# Getting Started

First of all, we need to import the unity package with the asset, if you haven't downloaded yet go [here](https://github.com/VRLabs/Modular-Shader-System/releases/latest) and grab the latest version.

Once everything is imported let's start creating our first modular shader! 

## Creating the Modular Shader

Everything starts with a `Modular shader` asset. This Asset contains all the basic information about the shader.

Create a new asset file by selecting `Assets > Create > Shader > VRLabs > Modular Shader > Modular Shader` (alternative you can bring up the `Assets` menu by right clicking in the project tab).

> [!WARNING]
> All assets specific for the Modular Shader System like Modular Shader, Shader module and Template assets should always go inside an `Editor` folder, since they're used only in editor to generate the shaders.

Now fill out the basic informations with the proper data (for more details about it, check [this page](xref:str-ModularShader#basic-informations)).

![inspector](/images/docs/GettingStarted/1.png)

After that, it's time to fill out some settings, let's start with the shader path. The shader path is just what you would fill in in the first line of the shader file and that defines the path of the shader when searching in the shader selector of the material.

After that there's the Custom editor value. If you're using a custom inspector you would fill it out with the `Namespace.ClassName` of the shader inspector you're going to use. In this example we'll keep it empty to let the shader use the default inspector.

![inspector](/images/docs/GettingStarted/2.png)

Now, let's get into the meat and let's make the base skeleton for our shader.
This skeleton will be in the `template asset` that will be placed in the `shader template` field.

Create the new template file by selecting `Assets > Create > Shader > VRLabs > Modular Shader > Template`, open the file in any text editor and paste the following code:

[!code[Main](Code/BaseTemplate.txt)]

In this template we set a couple of keywords for hooking up code for vertex and fragment functions, as well as function implementations and properties declarations.
To get more details about keywords in modular shader system, check out [this page](xref:str-Keywords) (we really encourage you to check it right now, as we're gonna talk about them more here).

After that just put the template into the `shader template` field.

![inspector](/images/docs/GettingStarted/3.png)

Now try to generate the shader to see what the system does (you will be prompted to select in which folder to put the generated shader).

[!code[Main](Code/ShaderCode.txt?range=1-4,9-47,55-58,64-72,75-82)]

Main notable thing: all keywords defined have disappeared.
This is because keywords defined are only used to generate the final shader, once that is done they get remove to avoid shader compilation error that would happen otherwise.

Other than that you can see that the shader has the correct name and does not implement a custom inspector since we did not set that field.

Also it currently uses properties ZTest, ZWrite and Cull that are not yet defined, let's fix that by filling out the parameters info.

![inspector](/images/docs/GettingStarted/4.png)

Now if we try to generate the shader again (if you reselect the same folder you will override the shader) you will see the shader now also has properties generated.

[!code[Main](Code/ShaderCode.txt?range=1-7,9-47,55-58,64-72,75-82&highlight=5-7)]

Perfect, now the shader has all the properties it uses. But it still doesn't output much since both the vertex and fragment shaders don't really anything at the moment. Now it's the time to make a module that will give some functionality to this shader.

> [!NOTE]
> Theoretically you could create a fully working shader just in the main template, and just leave the keywords as entry points for additional features.
>   
> The main downside for that is that the main hooks have to be inside the main template somewhere, meaning that all the relevant code (like the vertex and fragment function) will have to be there and not enclosed inside some `cginc` file, since the system doesn't really crawl into includes when it generates the shader, and therefore can't really know if inside one of them there's some keyword.
>   
> Using `cginc` file to put your shader code is not advised with the modular shader system, we instead promote the usage of templates in modules to obtain a similar result. (you are still able to use default includes just fine)

## Creating a Module

Time to create a module to give life to this shader, first create the module asset file by selecting `Assets > Create > Shader > VRLabs > Modular Shader > Shader Module`.

The informations area of the asset is similar to the modular shader assets, but with some key differences:
- The id **needs** to be filled in since it's going to be used by the system to check for duplicate modules, incompatibilities, and dependencies.
- There's a list of dependencies.
- There's a list of incompatibilities.

In our case we won't need to add any dependency or incompatibility, so we will just fill everything else with the proper information.

![inspector](/images/docs/GettingStarted/5.png)

Now time for the interesting bits, first of all, we need to make the vertex shader set the proper output. This is a good time to add a template and hook it to the `VERTEX_FUNCTION` keyword.

First let's create a new template to contain the vertex function implementation:

[!code[Main](Code/VertexTemplate.txt)]

And after that add a new template in the list and set the asset slot with the newly created template asset, and add some other data.

![inspector](/images/docs/GettingStarted/6.png)

The queue value is used to decide the order in which the templates are used to generate the shader. 
This is very important since a template is placed inside every keyword found at the *moment* the template is placed in, meaning that if templates that are added in a later stage have that same keywords, the template won't be added to those cause they're not there yet.
The order in which templates are evaluated and added is from lower queue to higher queue ones, if 2 templates are on the same queue, the first one will be based on the order of the relative modules in the shader, and in case 2 templates are in the same module and have the same queue, the one higher in the list goes first.

The `generate variant` toggle is used to define if the system has to generate different shaders to have the module this template is be enabled or disabled (we will talk more about enabling and disabling modules [in this page](xref:ThirdpartyModule#toggle-the-module)), this module will always be enabled so this setting is not used and should be left untoggled.
In our case even if the module was able to be enabled and disabled, this toggle would still be left unchecked, since the code in this template is inside a function and can be enabled and disabled by doing an conditional check, so there's no need to create multiple shaders.

The keywords list contains all the keywords this template will be hooked to, in our case it will be added only on the `VERTEX_FUNCTION` keyword.

Nice, now let's test it by adding the module in the modules list of the modular shader and generate the shader again.

![inspector](/images/docs/GettingStarted/7.png)

[!code[Main](Code/ShaderCode.txt?range=1-7,9-47,55-72,75-82&highlight=52-55)]

Now the shader outputs something! It's pitch black, but don't worry, we're going to give it some more color soon, by adding a function to the fragment function.

But first, we need a color property so that we can set a color from the inspector, so let's add it to the properties list of this module.

![inspector](/images/docs/GettingStarted/8.png)

Now time to create a function. Let's start that by creating another template asset (yes, functions need template assets as well, to get the function implementation).

[!code[Main](Code/FragmentFunction.txt)]

Template assets used for functions always need to have a void function with no parameters, in this case `ApplyColor`.

Now that we have the asset, let's fill the function data

![inspector](/images/docs/GettingStarted/9.png)

The name has to be the same as the name of the void function with no parameters mentioned before. 

the `Append After` field contains the hook for the function.
Unlike just templates this hook can be either keywords or other function declared in this or other modules. For this reason unlike other fields if you want to target a keyword here you have to keep the `#K#` prefix.

The `Queue` field works the same as in templates, with the difference being that everything is in the context of the same `Append After`.

The `Used Variables` list, just like the title suggests, contains all the variables that are going to be used in this function. in our case the only variable we need to put is the `_MyColor` variable (since the property is a `Color` property, it translated to a `float4` in shader).

> [!WARNING]
> While we do use the `FinalColor` variable in the function, you should not put it in the variables list because it is already declared by the template itself.
> in the variables list of functions you should never put variables that for some reason are already available to be used in that place of the shader.

The `Variable Keywords` and `Code Keywords` lists are used to tell where the function template code and the variables declarations should be placed. 
By default if the lists are empty the generator will try to put them in some default keywords, respectively being `DEFAULT_VARIABLES` and `DEFAULT_CODE`.
In our case those keywords are exactly where we need them to be, so we leave the lists empty.

Now, let's generate the shader again and see the result.

[!code[Main](Code/ShaderCode.txt?highlight=8,48,50-53,73)]

Now the shader is a fully functional shader that outputs the color we select with the property!

If you check the code you can see it added the _MyColor property, and placed its variable declaration and function definition in the place of the standard keywords, and created call to said function in the fragment shader.

From here you can add templates and features to this module to add features, or make a separate module for other features, the possibilities are endless.

## Templates vs Functions

Not the big question rises: when do you use templates and when do you use functions? don't they do the same thing by stitching code around?

Well, depends, they both do more or less the same thing, but in slightly different ways.

Templates are just a dumb "place code here now", so they're conceptually really simple and you can theoretically generate an entire shader with just that concept alone (some big VRChat shader developer is doing just that...).
But being simple also means having to manually deal with some issues like "is this function available in this bit of code?" "do i have to define the variable here or if i do it i'll get an error cause it's already defined?".

Functions on the other hand are a bit more complex to setup since they require you to tell them used variables, where to put them, where to put the code etc., but this also gives the system more power to handle some stuff, like variables being available without making duplicated.
Another big advantage is the possibility to use other functions as hook points for your functions, which means that the more functions are used, the more hooks are available to add more functions, giving you great flexibility, especially in big shader projects.

Also the debugging tools available for viewing functions are inherently better due the bigger amount of data available, which makes way easier to add functionality to someone else's modular shader with a custom module without too much prior knowledge of the shader design.

Of course functions also have some disadvantages outside of the longer initial setup. The main issue is that due to how they work, they can only be added to keywords inside a function implementation (like the fragment function in the example above).

Our initial intent with this subdivision was to have templates becoming the base skeleton of the final shader, with hook points inside the main functions to start adding modules with your own functions to add features.

