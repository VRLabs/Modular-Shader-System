---
uid: ThirdpartyModule
title: Create a Module for Third Parties
---

# Create a Module for Third Parties

Up until now we assumed that you've been working on your own modular shader where you know what each piece does and how it's designed already.

But if you're making a module to add a specific functionality to someone else's shader, you won't have the luxury of knowing exactly how it works, unless the creator of that shader made ample documentation for it.

Luckily we have the means to get at least enough informations to try and add some functionality of our own.

In this example we will use the modular shader built in [Getting Started](xref:GettingStarted) as a reference modular shader, but we will act like we didn't make it and we know nothing about it.

## The Modular Shader Debugger

Before starting doing anything, we probably need to look on the modular shader first. We could open the modular shader and shader module assets, but there's a better way to get a quick look: the Modular Shader Debugger.

Go to the menu `VRLabs > Modular Shader > Modular Shader Debugger`. A window will open with an object field and 2 (or more) tabs. put the modular shader you want to look a into that field.

### Template Graph

![inspector](/images/docs/ThirdpartyModule/1.png)

This first tab, called `Template Graph` is a node graph where all the template hierarchy is shown.
From here you can look at all the templates the modular shader uses with its current module setup.

Each node is a template, and on the left side it shows the keyword used to add it, while the right side contains all the keywords it declares.
Each connection indicates that the template on the right of the connection has been placed inside the the template on the left of the connection, by the keyword indicated by the port.

Our case is fairly simple, it's just the base template and an extra template.

> [!NOTE]
> In the case a template had multiple keywords to be placed on, in the graph there would be a number of copies of said template for each different keyword. 
> So if for example the template had been defined to be placed in both the `VERTEX_FUNCTION` and `FRAGMENT_FUNCTION` keywords we would have seen 2 different notes indicating the same template.
> 
> This is because the template graph is solving the template tree like it the shader generator would, so multiple uses of a template in different keywords will be instanced multiple times (multiple instances on the same keyword in different places of the same parent template are not duplicated).
> 
> Since it solves the template tree like the shader generator, it will also not include templates that are in the assets but are set up wrong and wouldn't end up inside the final shader, like templates with the queue set up wrong or similar. 
> 
>The tree is an image of what is actually used in the final result

By right clicking a node you have the possibility to preview its code and get a quick idea on what it does.

![inspector](/images/docs/ThirdpartyModule/2.png)

We have a general idea about the templates used by this modular shader, let's now go and look at the functions it may use.

### Function Timeline

![inspector](/images/docs/ThirdpartyModule/3.png)

The `Function Timeline` is a busier tab where you can see the order of the functions being called from a root keyword. you can select which keyword to select from the dropdown in the top, and clicking any function will show up its informations, as well as info about the module it is declared on and the template it uses.
In our case we only have one function in the `FRAGMENT_FUNCTION` keyword.

![inspector](/images/docs/ThirdpartyModule/4.png)

We can see that there's a `_MyColor` variable, and that there's a `FinalColor` variable being used in the implementation, despite not being in the variables list.
The author of this shader has either forgot to include `FinalColor` in the list or the variable is already statically defined in a template.

By looking at the templates we can see that the `FRAGMENT_FUNCTION` keyword is only on the base template, let's check the code quickly and see what's going on there.

![inspector](/images/docs/ThirdpartyModule/5.png)

Bingo! The `FinalColor` variable is here, and is initialized with 0, means that it's handled by the templates and we can use it as we please in our modules without declaring its usage.

Now we have a rough idea of how the area we want to hook in works, and we can start build our own module.

## Create the module

So, we've already done this before, so let's go through this quickly.

We want to create a module that adds a texture to the output of the material, tinted by the already available color.

So we create the asset, fill in the informations, add a property for our texture, and add a function that uses said texture to multiply the final color with it.

![inspector](/images/docs/ThirdpartyModule/6.png)

[!code[Main](Code/TextureFunction.txt)]

Now just add the module to the modular shader and generate the shader.
If everything went well, you can now use the newly generated shader with the texture slot.

## Toggle The Module

Texture samples can be expensive, and we don't want to use it always, so we want to do a toggle for it.

We could do it manually in the function code, but since the texture sample is everything the shader does to begin with, we can just toggle the entire module on and off.

This is fairly simple to do with the modular shader system, since each module can define an `Enabled` property, which will become a float property used to check if the module should be enabled or not.

Let's fill the data in and regenerate the shader.

![inspector](/images/docs/ThirdpartyModule/7.png)

We can now toggle on and off the texture without doing anything extra.
This is due to the generator encapsulating the function call into an `if` statement to check if the value of toggle property is the value we've given to to the module as a condition.

While generally branching in shaders is a bad idea, the compiler generally optimises where he can, and on top of this, if the shader you added the module in came with some editor scripts that use the capability of the generator to generate shaders optimized for their current material settings, whenever you disable the module and then run said script, the entire module code will not be included in the generated shader at all.

> [!NOTE]
> The modular shader system doesn't come with a 1 click script that optimizes the material for you, instead comes with an api to let you do that in your own setup,
> 
> We decided to go this way cause we think it gives more flexibility to shader creators to integrate the library in their own workflow. 


