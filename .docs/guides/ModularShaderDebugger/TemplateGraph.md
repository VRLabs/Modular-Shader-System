---
uid: dbg-TemplateGraph
title: Template Graph
---

# Template Graph

The template graph is a default tab of the [Modular Shader Debugger](xref:dbg-ModularShaderDebugger) that aims at providing a general view of how the templates are going to be linked together when generating the shader.

![Example template graph, thanks Poiyomi](/images/docs/ModularShaderDebugger/2.png)

The template tree starts from the left with a root node representing the main template defined in the modular shader asset (and another one to define the root of templates used for properties when the `Properties from templates` toggle of the modular shader is on).
From then templates are parsed like they would during shader generation and their relative nodes appended and connected with the keyword they defined. If a templated declares to be attached to multiple keywords, this is reflected in the graph by having multiple nodes attached, each to its keyword.

This process may end up not showing some templates that are declared in the asset, if that happens it usually is indicative of errors in some settings of those templates, since they would never be used when generating the shader to begin with.
This way the graph is representative of the structure of the generated shader.

## Template Node

![Template node, thanks Poiyomi](/images/docs/ModularShaderDebugger/3.png)

The template node is divided in 3 sections: the top bar, the used keyword on the left, and the declared keywords on the right.

There can be exceptions where either the left of right area is missing due to the node being the root (in the former case) or a template that doesn't declare keywords (the latter case).

![Template root node, thanks Poiyomi](/images/docs/ModularShaderDebugger/4.png)
![Template leaf node, thanks Poiyomi](/images/docs/ModularShaderDebugger/5.png)

The top bar includes 3 elements:
- The template name
- The module it comes from (in the form of the id of the module)
- Its queue value (on the right)

The left area will always contain the keyword that is used to place this template (with the exception of root nodes that don't have this area).

The right area contains all the keywords contained, regardless of if they're used for other templates or not. Internal keywords are indicated with an `(i)` at the end.

Right clicking the node will give you a popup menu with the option of showing off the content of the template, so you can quickly check what the template does.

![Template code view, thanks Poiyomi](/images/docs/ModularShaderDebugger/6.png)
