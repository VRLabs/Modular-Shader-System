---
uid: dbg-FunctionTimeline
title: Template Graph
---

# Function Timeline

The function timeline is a default tab of the [Modular Shader Debugger](xref:dbg-ModularShaderDebugger) that shows the functions flow inside a modular shader, divided by root keyword.

![tab](/images/docs/ModularShaderDebugger/7.png)

The is composed of the following areas:
- The timeline in the central area
- Selected function information
- Module information of the selected function
- Function's template code view

## Timeline

![Timeline](/images/docs/ModularShaderDebugger/11.png)

The main timeline shows the order in which each function will be placed inside the selected root keyword.

The order reflects the order in which the functions will be called in the shader, from left to right.

If multiple function elements have some overlap it means that there is a direct dependency between them, with the smaller one usually being the one that has the `AppendAfter` value set to the bigger one, this information can be shown by looking at the function's information after selecting the element.

You can select which timeline to show by selecting the root you want to see in the dropdown above the timeline.

### Timeline element

A single element contains the name of the function on the left, and it's queue on the right (the queue is always relative to its parent `AppendAfter`)

The selected element will have a cyan border, while elements with a yellow borders indicate a function that contains the variable selected in the `Selected Function information` area.

![unselected item](/images/docs/ModularShaderDebugger/8.png)

![selected item](/images/docs/ModularShaderDebugger/9.png)

![item with selected variable](/images/docs/ModularShaderDebugger/10.png)

## Selected function information

![Selected function info](/images/docs/ModularShaderDebugger/12.png)

This area shows informations relative to the function, such as the name, queue, and where it's appended.

It also shows its variables and in which keywords variables and implementation are put on.

Selecting a variable will show which other functions in the timeline that use the same variable, giving you the possibility to check where it's used and for what.

## Function's module base info

![Module info](/images/docs/ModularShaderDebugger/13.png)

This area shows some basic informations about the module where the selected function is defines, and give you a quick way to select the module asset for further inspection if needed.

## Function code template

![Module info](/images/docs/ModularShaderDebugger/14.png)

This area shows the template containing the implementation of the selected function.
