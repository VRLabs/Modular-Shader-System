﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    public static class ShaderGenerator
    {
        /// <summary>
        /// Generates a shader with all shader variants
        /// </summary>
        /// <param name="path">path of the folder to put the shader files</param>
        /// <param name="shader">Modular shader to use</param>
        /// <param name="hideVariants">Hide variants from the shader selector on the material, showing only the shader with all variants disabled from the menu</param>
        public static void GenerateShader(string path, ModularShader shader, bool hideVariants = false)
        {
            GenerateShader(path, shader, null, hideVariants);
        }

        /// <summary>
        /// Generates a shader with all shader variants, you can give a custom PostGeneration action to handle the shader result for some custom modifications (build keywords are still available at this stage)
        /// </summary>
        /// <param name="path">path of the folder to put the shader files</param>
        /// <param name="shader">Modular shader to use</param>
        /// <param name="postGeneration">Actions to performs post generation and before cleanup </param>
        /// <param name="hideVariants">Hide variants from the shader selector on the material, showing only the shader with all variants disabled from the menu</param>
        public static void GenerateShader(string path, ModularShader shader, Action<StringBuilder, ShaderContext> postGeneration, bool hideVariants = false)
        {
            
            path = GetPathRelativeToProject(path);

            var modules = FindAllModules(shader);
            
            // Countermeasure for unity dogshit scriptedImporter assets reimport/update system
            var freshAssets = new Dictionary<TemplateAsset, TemplateAsset>();

            freshAssets.AddFreshShaderToList(shader.ShaderTemplate);
            freshAssets.AddFreshShaderToList(shader.ShaderPropertiesTemplate);

            foreach (var template in modules.SelectMany(x => x.Templates))
                freshAssets.AddFreshShaderToList(template.Template);

            foreach (var function in modules.SelectMany(x => x.Functions))
                freshAssets.AddFreshShaderToList(function.ShaderFunctionCode);
            
            
            var possibleVariants = GetShaderVariants(modules);
            var contexts = new List<ShaderContext>();
            var completePropertiesBlock = GetPropertiesBlock(shader, modules, freshAssets);
            
            foreach (var variant in possibleVariants)
            {
                contexts.Add(new ShaderContext
                {
                    Shader = shader,
                    PostGeneration = postGeneration,
                    ActiveEnablers = variant,
                    FreshAssets = freshAssets,
                    FilePath = path,
                    PropertiesBlock = completePropertiesBlock,
                    AreVariantsHidden = hideVariants
                });
            }
            
            contexts.AsParallel().ForAll(x => x.GenerateShader());
            try
            {
                AssetDatabase.StartAssetEditing();
                
                if (shader.LastGeneratedShaders != null)
                {
                    foreach (Shader generatedShader in shader.LastGeneratedShaders.Where(x => x != null))
                    {
                        string assetPath = AssetDatabase.GetAssetPath(generatedShader);
                        if (string.IsNullOrWhiteSpace(assetPath))
                            File.Delete(assetPath);
                    }
                }

                shader.LastGeneratedShaders = new List<Shader>();
                
                foreach (var context in contexts)
                    File.WriteAllText($"{path}/" + context.VariantFileName, context.ShaderFile.ToString());
            }
            finally
            {
                // To make sure the AssetDatabase doesn't break out
                AssetDatabase.StopAssetEditing();
            }
            
            AssetDatabase.Refresh();
            ApplyDefaultTextures(contexts);
            
            foreach (var context in contexts)
                shader.LastGeneratedShaders.Add(AssetDatabase.LoadAssetAtPath<Shader>($"{path}/" + context.VariantFileName));
            AssetDatabase.Refresh();
        }

        private static string GetPathRelativeToProject(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"The folder \"{path}\" is not found");

            if (!path.Contains(Application.dataPath) && !path.StartsWith("Assets"))
                throw new DirectoryNotFoundException($"The folder \"{path}\" is not part of the unity project");

            if(!path.StartsWith("Assets"))
                path = path.Replace(Application.dataPath, "Assets");
            
            return path;
        }

        /// <summary>
        /// Generates a shader for selected materials
        /// </summary>
        /// <param name="path">path for the shader files</param>
        /// <param name="shader">Modular shader to use</param>
        /// <param name="materials">List of materials given</param>
        /// <param name="postGeneration">Actions to performs post generation and before cleanup </param>
        public static void GenerateMinimalShader(string path, ModularShader shader, IEnumerable<Material> materials, Action<StringBuilder, ShaderContext> postGeneration = null)
        {
            path = GetPathRelativeToProject(path);
            
            var modules = FindAllModules(shader);
            var possibleVariants = GetMinimalVariants(modules, materials);
            var contexts = new List<ShaderContext>();
            
            foreach (var (variant, variantMaterials) in possibleVariants)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(variantMaterials[0], out string guid, out long  _);
                contexts.Add(new ShaderContext
                {
                    Shader = shader,
                    PostGeneration = postGeneration,
                    ActiveEnablers = variant,
                    FilePath = path,
                    OptimizedShader = true,
                    Materials = variantMaterials,
                    Guid = guid
                });
            }

            contexts.GenerateMinimalShaders();
        }

        /// <summary>
        /// Enqueues shaders to generate
        /// </summary>
        /// <param name="path">path for the shader files</param>
        /// <param name="shader">Modular shader to use</param>
        /// <param name="materials">List of materials given</param>
        /// <returns>A list of the shaderContexts</returns>
        /// <param name="postGeneration">Actions to performs post generation and before cleanup </param>
        public static List<ShaderContext> EnqueueShadersToGenerate(string path, ModularShader shader, IEnumerable<Material> materials, Action<StringBuilder, ShaderContext> postGeneration = null)
        {
            path = GetPathRelativeToProject(path);
            
            var modules = FindAllModules(shader);
            var possibleVariants = GetMinimalVariants(modules, materials);
            var contexts = new List<ShaderContext>();
            
            foreach (var (variant, variantMaterials) in possibleVariants)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(variantMaterials[0], out string guid, out long  _);
                contexts.Add(new ShaderContext
                {
                    Shader = shader,
                    PostGeneration = postGeneration,
                    ActiveEnablers = variant,
                    FilePath = path,
                    OptimizedShader = true,
                    Materials = variantMaterials,
                    Guid = guid
                });
            }

            return contexts;
        }

        /// <summary>
        /// Generates shaders from the given list of contexts
        /// </summary>
        /// <param name="contexts"></param>
        public static void GenerateMinimalShaders(this List<ShaderContext> contexts)
        {
            if (contexts == null || contexts.Count == 0) return;

            // Still Countermeasure for unity dogshit scriptedImporter assets reimport/update system
            var alreadyDoneShaders = new List<ModularShader>();
            
            var freshAssets = new Dictionary<TemplateAsset, TemplateAsset>();
            
            foreach (var context in contexts)
            {
                context.FreshAssets = freshAssets;
                if (alreadyDoneShaders.Contains(context.Shader)) continue;
                
                var shader = context.Shader;
                var modules = FindAllModules(shader);
                
                freshAssets.AddFreshShaderToList(shader.ShaderTemplate);
                freshAssets.AddFreshShaderToList(shader.ShaderPropertiesTemplate);

                foreach (var template in modules.SelectMany(x => x.Templates))
                    freshAssets.AddFreshShaderToList(template.Template);

                foreach (var function in modules.SelectMany(x => x.Functions))
                    freshAssets.AddFreshShaderToList(function.ShaderFunctionCode);
                
                alreadyDoneShaders.Add(shader);
            }
            
            // ReSharper disable once PossibleLossOfFraction
            EditorUtility.DisplayProgressBar("Generating Optimized Shaders", "generating shader files", 1 / (contexts.Count + 3));
            contexts.AsParallel().ForAll(x => x.GenerateShader());
            try
            {
                AssetDatabase.StartAssetEditing();
                int i = 0;
                foreach (var context in contexts)
                {
                    EditorUtility.DisplayProgressBar("Generating Optimized Shaders", "Saving " + context.VariantFileName, 1 + i / (contexts.Count + 3));
                    File.WriteAllText($"{context.FilePath}/" + context.VariantFileName, context.ShaderFile.ToString());
                    i++;
                }
            }
            finally
            {
                EditorUtility.DisplayProgressBar("Generating Optimized Shaders", "waiting for unity to compile shaders", contexts.Count - 2 / (contexts.Count + 3));
                // To make sure the AssetDatabase doesn't break out
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            
            ApplyDefaultTextures(contexts);

            EditorUtility.DisplayProgressBar("Generating Optimized Shaders", "applying shaders to materials", contexts.Count - 1 / (contexts.Count + 3));
            foreach (var context in contexts)
            {
                var shader = Shader.Find(context.ShaderName);
                foreach (var material in context.Materials)
                {
                    material.shader = shader;
                }
            }
            
            EditorUtility.ClearProgressBar();
        }

        // Gets a list of all possible variants
        private static List<Dictionary<string, int>> GetShaderVariants(List<ShaderModule> modules)
        {
            var dictionary = new Dictionary<string, List<int>>();
            foreach (ShaderModule module in modules)
            {
                if (module == null) continue;
                foreach (EnableProperty property in module.EnableProperties)
                {
                    if (property == null || string.IsNullOrWhiteSpace(property.Name) ||
                        !(module.Templates?.Any(x => x.NeedsVariant) ?? false)) continue;

                    if (dictionary.ContainsKey(property.Name))
                        dictionary[property.Name].Add(property.EnableValue);
                    else
                        dictionary.Add(property.Name, new List<int>(new[] { property.EnableValue }));
                }
            }

            var keys = dictionary.Keys.ToList();

            foreach (KeyValuePair<string,List<int>> keyValuePair in dictionary)
                if(!keyValuePair.Value.Contains(0))
                    keyValuePair.Value.Insert(0,0);

            var states = new List<Dictionary<string, int>>();
            UnrollVariants(states, new Dictionary<string, int>(), dictionary, keys);

            return states;
        }
        
        private static List<(Dictionary<string, int>, List<Material>)> GetMinimalVariants(List<ShaderModule> modules, IEnumerable<Material> materials)
        {
            var enablers = new List<string>();
            foreach (ShaderModule module in modules)
            {
                if (module == null) continue;
                foreach (EnableProperty property in module.EnableProperties)
                {
                    if (property == null || string.IsNullOrWhiteSpace(property.Name)) continue;

                    enablers.Add(property.Name);
                }
            }

            enablers = enablers.Distinct().ToList();

            var states = new List<(Dictionary<string, int>, List<Material>)>();
            foreach (Material material in materials)
            {
                var state = new Dictionary<string, int>();
                foreach (string enabler in enablers)
                    state.Add(enabler, (int)material.GetFloat(enabler));

                var equalState = states.Where(x =>
                {
                    var keys = state.Keys;
                    foreach (string key in keys)
                        if (x.Item1[key] != state[key])
                            return false;

                    return true;
                }).FirstOrDefault();
                
                if(equalState == (null, null))
                    states.Add((state, new List<Material>(new [] {material})));
                else
                    equalState.Item2.Add(material);
            }

            return states;
        }

        // Unrolls all possible combinations of modules that need a separate variant.
        private static void UnrollVariants(ICollection<Dictionary<string, int>> states, Dictionary<string, int> current, IReadOnlyDictionary<string, List<int>> dictionary, IReadOnlyList<string> keys)
        {
            if (current.Count == keys.Count)
            {
                states.Add(current);
                return;
            }
            foreach (var value in dictionary[keys[current.Count]])
            {
                var next = new Dictionary<string, int>(current);
                next[keys[current.Count]] = value;
                UnrollVariants(states, next, dictionary, keys);
            }
        }

        // Get code based on active enablers
        public static string GetVariantCode(Dictionary<string, int> activeEnablers)
        {
            var keys = activeEnablers.Keys.OrderBy(x => x).ToList();
            bool isAllZeroes = true;
            var b = new StringBuilder();
            foreach (string key in keys)
            {
                if (activeEnablers[key] != 0) isAllZeroes = false;
                b.Append($"-{activeEnablers[key]}");
            }

            return isAllZeroes ? "" : b.ToString();
        }
        
        // Loads A new version of the asset into the dictionary if not already available
        private static void AddFreshShaderToList(this Dictionary<TemplateAsset, TemplateAsset> dictionary, TemplateAsset asset)
        {
            if ((object)asset == null) return;
            if (dictionary.ContainsKey(asset)) return;
            string assetName = asset.name;
            string assetPath = AssetDatabase.GetAssetPath(asset);
            var genericAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            TemplateAsset template = null;
            switch (genericAsset)
            {
                case TemplateCollectionAsset collection:
                    template = collection.Templates.FirstOrDefault(x => assetName.Equals(asset.name));
                    break;
                case TemplateAsset t:
                    template = t;
                    break;
            }
            dictionary.Add(asset, template);
        }
        
        // Retrieves the new version of the asset from the dictionary if available
        private static TemplateAsset GetTemplate(this Dictionary<TemplateAsset, TemplateAsset> dictionary, TemplateAsset asset)
        {
            if ((object)asset == null) return null;
            return dictionary.TryGetValue(asset, out TemplateAsset result) ? result : null;
        }

        public class ShaderContext
        {
            public ModularShader Shader;
            public Dictionary<string, int> ActiveEnablers;
            public Dictionary<TemplateAsset, TemplateAsset> FreshAssets;
            public Action<StringBuilder, ShaderContext> PostGeneration;
            private List<EnableProperty> _liveUpdateEnablers;
            public string FilePath;
            public string VariantFileName;
            public string VariantName;
            public string ShaderName;
            public string PropertiesBlock;
            public bool AreVariantsHidden;
            public bool OptimizedShader;
            public List<Material> Materials;
            public StringBuilder ShaderFile;
            private List<ShaderModule> _modules;
            private List<ShaderFunction> _functions;
            private List<ShaderFunction> _reorderedFunctions;
            private Dictionary<ShaderFunction, ShaderModule> _modulesByFunctions;
            public string Guid;

            public List<ShaderModule> Modules => _modules;

            public void GenerateShader()
            {
                _modules = FindActiveModules(Shader, ActiveEnablers);
                GetLiveUpdateEnablers();
                ShaderFile = new StringBuilder();
                VariantName = GetVariantCode(ActiveEnablers);
                VariantFileName = OptimizedShader ? 
                    $"{Shader.Name}{(string.IsNullOrEmpty(VariantName) ? "" : $"-g-{Guid}")}.shader" :
                    $"{Shader.Name}{(string.IsNullOrEmpty(VariantName) ? "" : $"-v{VariantName}")}.shader";

                VariantFileName = string.Join("_", VariantFileName.Split(Path.GetInvalidFileNameChars()));

                if (OptimizedShader)
                    ShaderName = $"Hidden/{Shader.ShaderPath}-g-{Guid}";
                else if (AreVariantsHidden && !string.IsNullOrEmpty(VariantName))
                    ShaderName = $"Hidden/{Shader.ShaderPath}-v{VariantName}";
                else
                    ShaderName = $"{Shader.ShaderPath}{VariantName}";
                
                ShaderFile.AppendLine($"Shader \"{ShaderName}\"");

                ShaderFile.AppendLine("{");

                // If the properties block value is empty, assume that the context is generating an optimised shader.
                ShaderFile.Append(string.IsNullOrEmpty(PropertiesBlock) ? GetPropertiesBlock(Shader, _modules, FreshAssets, false) : PropertiesBlock);

                WriteShaderSkeleton();
                
                // Get functions currently used in this variant.
                _functions = new List<ShaderFunction>();
                _reorderedFunctions = new List<ShaderFunction>();
                _modulesByFunctions = new Dictionary<ShaderFunction, ShaderModule>();
                foreach (var module in _modules)
                {
                    _functions.AddRange(module.Functions);
                    foreach (ShaderFunction function in module.Functions)
                    {
                        _modulesByFunctions.Add(function, module);
                    }
                }

                WriteVariablesToKeywords();
                WriteFunctionCallsToKeywords();
                WriteFunctionsToKeywords();

                if (!string.IsNullOrWhiteSpace(Shader.CustomEditor))
                    ShaderFile.AppendLine($"CustomEditor \"{Shader.CustomEditor}\"");
                ShaderFile.AppendLine("}");

                PostGeneration?.Invoke(ShaderFile, this);

                RemoveKeywords();

                ShaderFile.Replace("\r\n", "\n");

                ShaderFile = CleanupShaderFile(ShaderFile);
            }
            
            private void GetLiveUpdateEnablers()
            {
                _liveUpdateEnablers = new List<EnableProperty>();
                var staticEnablers = ActiveEnablers.Keys.ToList();
                foreach (var property in _modules.SelectMany(x => x.EnableProperties))
                {
                    if(property != null && !string.IsNullOrWhiteSpace(property.Name) && !staticEnablers.Contains(property.Name))
                        _liveUpdateEnablers.Add(property);
                }

                _liveUpdateEnablers = _liveUpdateEnablers.Distinct().ToList();
            }

            private void WriteFunctionCallsToKeywords()
            {
                foreach (var startKeyword in _functions.Where(x => x.AppendAfter?.StartsWith("#K#") ?? false).Select(x => x.AppendAfter).Distinct())
                {
                    if (!ShaderFile.Contains(startKeyword)) continue;

                    var callSequence = new StringBuilder();
                    WriteFunctionCallSequence(callSequence, startKeyword);
                    //callSequence.AppendLine(startKeyword);
                    var m = Regex.Matches(ShaderFile.ToString(), $@"{startKeyword}(\s|$)", RegexOptions.Multiline);
                    for (int i = m.Count - 1; i >= 0; i--)
                        ShaderFile.Insert(m[i].Index, callSequence.ToString()); 
                    //ShaderFile.Replace(startKeyword, callSequence.ToString());
                }
            }

            // Write down all templates to generate the initial keyworded skeleton of the shader
            private void WriteShaderSkeleton()
            {
                ShaderFile.AppendLine("SubShader");
                ShaderFile.AppendLine("{");

                ShaderFile.AppendLine(FreshAssets.GetTemplate(Shader.ShaderTemplate).Template);

                Dictionary<ModuleTemplate, ShaderModule> moduleByTemplate = new Dictionary<ModuleTemplate, ShaderModule>();
                Dictionary<(string, string), string> convertedKeyword = new Dictionary<(string, string), string>();
                int instanceCounter = 0;

                foreach (var module in _modules)
                    foreach (var template in module.Templates)
                        moduleByTemplate.Add(template, module);

                //foreach (var module in _modules)
                //{
                    foreach (var template in _modules.SelectMany(x => x.Templates).OrderBy(x => x.Queue))
                    {
                        var freshTemplate = FreshAssets.GetTemplate(template.Template);
                        var module = moduleByTemplate[template];
                        if (freshTemplate == null) continue;
                        bool hasEnabler = module.EnableProperties.Any(x => x != null && !string.IsNullOrEmpty(x.Name));
                        bool isFilteredIn = hasEnabler && module.EnableProperties.All(x => (x == null || string.IsNullOrEmpty(x.Name)) || ActiveEnablers.TryGetValue(x.Name, out _));
                        bool needsIf = hasEnabler && !isFilteredIn && !template.NeedsVariant;
                        var tmp = new StringBuilder();

                        if (!needsIf)
                        {
                            tmp.AppendLine(freshTemplate.Template);
                        }

                        else
                        {
                            string condition = string.Join(" && ", module.EnableProperties
                                .Where(x => (x != null && !string.IsNullOrEmpty(x.Name)) && !ActiveEnablers.TryGetValue(x.Name, out _))
                                .Select(x => $"{x.Name} == {x.EnableValue}"));
                            tmp.AppendLine($"if({condition})");
                            tmp.AppendLine("{");
                            tmp.AppendLine(freshTemplate.Template);
                            tmp.AppendLine("}");
                        }
                        
                        MatchCollection mki = Regex.Matches(tmp.ToString(), @"#KI#\S*", RegexOptions.Multiline);
                        for (int i = mki.Count - 1; i >= 0; i--)
                        {
                            string newKeyword;
                            if (convertedKeyword.TryGetValue((module.Id, mki[i].Value), out string replacedKeyword))
                            {
                                newKeyword = replacedKeyword;
                            }
                            else
                            {
                                newKeyword = $"{mki[i].Value}{instanceCounter++}";
                                convertedKeyword.Add((module.Id, mki[i].Value), newKeyword);
                            }
                            tmp.Replace(mki[i].Value, newKeyword);
                        }

                        foreach (var keyword in template.Keywords.Count == 0 ? new[] { MSSConstants.DEFAULT_CODE_KEYWORD } : template.Keywords.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray())
                        {
                            MatchCollection m = Regex.Matches(ShaderFile.ToString(), $@"#K#{keyword}(\s|$)", RegexOptions.Multiline);
                            for (int i = m.Count - 1; i >= 0; i--)
                                ShaderFile.Insert(m[i].Index, tmp.ToString());

                            if (convertedKeyword.TryGetValue((module.Id, $@"#KI#{keyword}"), out string replacedKeyword))
                            {
                                m = Regex.Matches(ShaderFile.ToString(), $@"{replacedKeyword}(\s|$)", RegexOptions.Multiline);
                                for (int i = m.Count - 1; i >= 0; i--)
                                    ShaderFile.Insert(m[i].Index, tmp.ToString());   
                            }
                        }
                    }
                    MatchCollection mkr = Regex.Matches(ShaderFile.ToString(), @"#KI#\S*", RegexOptions.Multiline);
                    for (int i = mkr.Count - 1; i >= 0; i--)
                        ShaderFile.Remove(mkr[i].Index, mkr[i].Length);
                //}
                
                ShaderFile.AppendLine("}");
            }
            
            // Writes variables to keywords
            private void WriteVariablesToKeywords()
            {
                var variableDeclarations = new Dictionary<string,List<Variable>>();

                foreach (ShaderFunction function in _functions)
                {
                    if (function.VariableKeywords.Count > 0)
                    {
                        foreach (string keyword in function.VariableKeywords)
                        {
                            if (!variableDeclarations.ContainsKey(keyword))
                                variableDeclarations.Add(keyword, new List<Variable>());

                            foreach (Variable variable in function.UsedVariables)
                                variableDeclarations[keyword].Add(variable);
                        }
                    }
                    else
                    {
                        if (!variableDeclarations.ContainsKey(MSSConstants.DEFAULT_VARIABLES_KEYWORD))
                            variableDeclarations.Add(MSSConstants.DEFAULT_VARIABLES_KEYWORD, new List<Variable>());

                        foreach (Variable variable in function.UsedVariables)
                            variableDeclarations[MSSConstants.DEFAULT_VARIABLES_KEYWORD].Add(variable);
                    }
                }

                foreach (var declaration in variableDeclarations)
                {
                    declaration.Value.AddRange(_liveUpdateEnablers.Select(x => x.ToVariable()));
                    var decCode = string.Join("\n", declaration.Value.Distinct().OrderBy(x => x.Type).Select(x => x.GetDefinition())) + "\n\n";
                    MatchCollection m = Regex.Matches(ShaderFile.ToString(), $@"#K#{declaration.Key}\s", RegexOptions.Multiline);
                    for (int i = m.Count - 1; i >= 0; i--)
                        ShaderFile.Insert(m[i].Index, decCode);   
                }
            }
            
            // Writes function declarations to keywords
            private void WriteFunctionsToKeywords()
            {
                var keywordedCode = new Dictionary<string,(StringBuilder, List<TemplateAsset>)>();

                foreach (ShaderFunction function in _reorderedFunctions)
                {
                    var freshAsset = FreshAssets.GetTemplate(function.ShaderFunctionCode);
                    if (function.CodeKeywords.Count > 0)
                    {
                        foreach (string keyword in function.CodeKeywords)
                        {
                            if (!keywordedCode.ContainsKey(keyword))
                                keywordedCode.Add(keyword, (new StringBuilder(), new List<TemplateAsset>()));

                            if (freshAsset == null) continue;
                            (StringBuilder builder, List<TemplateAsset> assets) = keywordedCode[keyword];
                            if (assets.Contains(freshAsset)) continue;
                            builder.AppendLine(freshAsset.Template);
                            assets.Add(freshAsset);
                        }
                    }
                    else
                    {
                        if (!keywordedCode.ContainsKey(MSSConstants.DEFAULT_CODE_KEYWORD))
                            keywordedCode.Add(MSSConstants.DEFAULT_CODE_KEYWORD, (new StringBuilder(), new List<TemplateAsset>()));
                        
                        if (freshAsset == null) continue;
                        (StringBuilder builder, List<TemplateAsset> assets) = keywordedCode[MSSConstants.DEFAULT_CODE_KEYWORD];
                        if (assets.Contains(freshAsset)) continue;
                        builder.AppendLine(freshAsset.Template);
                        assets.Add(freshAsset);
                    }
                }

                foreach (var code in keywordedCode)
                {
                    MatchCollection m = Regex.Matches(ShaderFile.ToString(), $@"#K#{code.Key}\s", RegexOptions.Multiline);
                    for (int i = m.Count - 1; i >= 0; i--)
                        ShaderFile.Insert(m[i].Index, code.Value.Item1.ToString());   
                }
            }

            // Write sequence of functions.
            private void WriteFunctionCallSequence(StringBuilder callSequence, string appendAfter)
            {
                foreach (var function in _functions.Where(x => x.AppendAfter.Equals(appendAfter)).OrderBy(x => x.Queue))
                {
                    _reorderedFunctions.Add(function);
                    ShaderModule module = _modulesByFunctions[function];
                    
                    bool hasEnabler = module.EnableProperties.Any(x => x != null && !string.IsNullOrEmpty(x.Name));
                    bool isFilteredIn = hasEnabler && module.EnableProperties.All(x => (x == null || string.IsNullOrEmpty(x.Name)) || ActiveEnablers.TryGetValue(x.Name, out _));
                    bool needsIf = hasEnabler && !isFilteredIn;

                    if (needsIf)
                    {
                        string condition = string.Join(" && ", module.EnableProperties
                            .Where(x => (x != null && !string.IsNullOrEmpty(x.Name)) && !ActiveEnablers.TryGetValue(x.Name, out _))
                            .Select(x => $"{x.Name} == {x.EnableValue}"));
                        callSequence.AppendLine($"if({condition})");
                        callSequence.AppendLine("{");
                    }
                    
                    callSequence.AppendLine($"{function.Name}();");
                    WriteFunctionCallSequence(callSequence, function.Name);
                    
                    if (needsIf)
                        callSequence.AppendLine("}");
                }
            }
            
            // Remove keywords from string
            private void RemoveKeywords()
            {
                int current = 0;

                while (current < ShaderFile.Length)
                {
                    if (ShaderFile.Length >= current + 3 && ShaderFile[current] == '#' && ShaderFile[current + 1] == 'K' && 
                        ShaderFile[current + 2] == '#')
                    {
                        int end = current+3;
                        bool stillToRemove = true;
                        while (end < ShaderFile.Length)
                        {
                            if (char.IsWhiteSpace(ShaderFile[end]))
                            {
                                ShaderFile.Remove(current, end - current);
                                stillToRemove = false;
                                break;
                            }

                            end++;
                        }
                        if(stillToRemove)
                            ShaderFile.Remove(current, end - current);
                    }
                    
                    current++;
                }
            }
            
            // Check a line of the property block
            private static bool CheckPropertyBlockLine(StringBuilder builder, StringReader reader, string line, ref int tabs, ref bool deleteEmptyLine)
            {
                string ln = null;
                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    if (deleteEmptyLine)
                        return false;
                    deleteEmptyLine = true;
                }
                else
                {
                    deleteEmptyLine = false;
                }

                if (line.StartsWith("}") && (ln = reader.ReadLine()) != null && ln.Trim().StartsWith("SubShader"))
                    tabs--;
                builder.AppendLineTabbed(tabs, line);

                if (!string.IsNullOrWhiteSpace(ln))
                    if (CheckPropertyBlockLine(builder, reader, ln, ref tabs, ref deleteEmptyLine))
                        return true;

                if (line.StartsWith("}") && ln != null && ln.Trim().StartsWith("SubShader"))
                    return true;
                return false;
            }
            
            // Cleanup the final shader file by indenting it decently
            private static StringBuilder CleanupShaderFile(StringBuilder shaderVariant)
            {
                var finalFile = new StringBuilder();
                using (var sr = new StringReader(shaderVariant.ToString()))
                {
                    string line;
                    int tabs = 0;
                    bool deleteEmptyLine = false;
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (string.IsNullOrEmpty(line))
                        {
                            if (deleteEmptyLine)
                                continue;
                            deleteEmptyLine = true;
                        }
                        else
                        {
                            deleteEmptyLine = false;
                        }

                        // Special handling for the properties block, there should never be indentation inside here
                        if (line.StartsWith("Properties"))
                        {
                            finalFile.AppendLineTabbed(tabs, line);
                            string ln = sr.ReadLine()?.Trim();      // When the previous line is the one containing "Properties" we always know 
                            finalFile.AppendLineTabbed(tabs, ln);   // that the next line is "{" so we just write it down before increasing the tabs
                            tabs++;
                            while ((ln = sr.ReadLine()) != null)    // we should be escaping this loop way before actually meeting the condition, but you never know
                            {
                                if (CheckPropertyBlockLine(finalFile, sr, ln, ref tabs, ref deleteEmptyLine))
                                    break;
                            }
                            continue;
                        }

                        if (!line.StartsWith("//") && (line.StartsWith("}") || line.EndsWith("}") && !line.Contains("{")))
                            tabs--;
                        finalFile.AppendLineTabbed(tabs, line);
                        if (!line.StartsWith("//") && (line.StartsWith("{") || line.EndsWith("{")))
                            tabs++;
                    }
                }

                return finalFile;
            }
        }

        // Retrieves properties block based on given modules
        private static string GetPropertiesBlock(ModularShader shader, List<ShaderModule> modules, Dictionary<TemplateAsset, TemplateAsset> freshAssets, bool includeEnablers = true)
        {
            var block = new StringBuilder();
            block.AppendLine("Properties");
            block.AppendLine("{");

            if (shader.UseTemplatesForProperties)
            {
                var freshTemplate = freshAssets.GetTemplate(shader.ShaderPropertiesTemplate);
                if (freshTemplate != null)
                    block.AppendLine(freshTemplate.Template);

                block.AppendLine($"#K#{MSSConstants.TEMPLATE_PROPERTIES_KEYWORD}");
            }
            else
            {
                List<Property> properties = new List<Property>();

                properties.AddRange(shader.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Name) || x.Attributes.Count > 0));

                foreach (var module in modules.Where(x => x != null))
                {
                    properties.AddRange(module.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Name) || x.Attributes.Count > 0));
                    if (module.EnableProperties.Count > 0 && includeEnablers)
                        properties.AddRange(module.EnableProperties.Where(x => !string.IsNullOrWhiteSpace(x.Name)));
                    
                }

                foreach (var prop in properties.Distinct())
                {
                    if (string.IsNullOrWhiteSpace(prop.Type) && !string.IsNullOrWhiteSpace(prop.Name))
                    {
                        prop.Type = "Float";
                        prop.DefaultValue = "0.0";
                    }

                    string attributes = prop.Attributes.Count == 0 ? "" : $"[{string.Join("][", prop.Attributes)}]";
                    block.AppendLine(string.IsNullOrWhiteSpace(prop.Name) ? attributes : $"{attributes} {prop.Name}(\"{prop.DisplayName}\", {prop.Type}) = {prop.DefaultValue}");
                }
            }

            block.AppendLine("}");
            return block.ToString();
        }
        
        private static void ApplyDefaultTextures(List<ShaderContext> contexts)
        {
            foreach (var context in contexts)
            {
                var importedShader = AssetImporter.GetAtPath($"{context.FilePath}/" + context.VariantFileName) as ShaderImporter;
                var customTextures = context.Modules.SelectMany(x => x.Properties).Where(x => x.DefaultTextureAsset != null).ToList();
                customTextures.AddRange(context.Shader.Properties.Where(x => x.DefaultTextureAsset != null).ToList());
                if (importedShader != null)
                {
                    importedShader.SetDefaultTextures(customTextures.Select(x => x.Name).ToArray(), customTextures.Select(x => x.DefaultTextureAsset).ToArray());
                    importedShader.SetNonModifiableTextures(customTextures.Select(x => x.Name).ToArray(), customTextures.Select(x => x.DefaultTextureAsset).ToArray());
                }
                AssetDatabase.ImportAsset($"{context.FilePath}/" + context.VariantFileName);
            }
        }
        
        /// <summary>
        /// Find all modules inside a specified shader.
        /// </summary>
        /// <param name="shader">Modular shader to check</param>
        /// <returns>A list of <see cref="ShaderModule"/> inside this shader</returns>
        public static List<ShaderModule> FindAllModules(ModularShader shader)
        {
            List<ShaderModule> modules = new List<ShaderModule>();
            if (shader == null) return modules;
            modules.AddRange(shader.BaseModules.Where(x => x != null));
            modules.AddRange(shader.AdditionalModules.Where(x => x != null));
            return modules;
        }

        /// <summary>
        /// Find all properties declared by the shader and its current modules
        /// </summary>
        /// <param name="shader">Modular shader to check</param>
        /// <returns>A list of <see cref="Property"/> contained in this shader</returns>
        public static List<Property> FindAllProperties(ModularShader shader)
        {
            List<Property> properties = new List<Property>();
            if (shader == null) return properties;

            properties.AddRange(shader.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Name) || x.Attributes.Count == 0));

            foreach (var module in shader.BaseModules.Where(x => x != null))
            {
                properties.AddRange(module.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Name) || x.Attributes.Count == 0));
                if (module.EnableProperties.Count > 0)
                    properties.AddRange(module.EnableProperties.Where(x => !string.IsNullOrWhiteSpace(x.Name)));
            }

            foreach (var module in shader.AdditionalModules.Where(x => x != null))
            {
                properties.AddRange(module.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Name) || x.Attributes.Count == 0));
                if (module.EnableProperties.Count > 0)
                    properties.AddRange(module.EnableProperties.Where(x => !string.IsNullOrWhiteSpace(x.Name)));
            }

            return properties.Distinct().ToList();
        }
        
        /// <summary>
        /// Find all functions declared by all the modules inside a specified shader
        /// </summary>
        /// <param name="shader">Modular shader to check</param>
        /// <returns>A list of <see cref="ShaderFunction"/> inside this shader</returns>
        public static List<ShaderFunction> FindAllFunctions(ModularShader shader)
        {
            var functions = new List<ShaderFunction>();
            if (shader == null) return functions;
            foreach (var module in shader.BaseModules)
                functions.AddRange(module.Functions);

            foreach (var module in shader.AdditionalModules)
                functions.AddRange(module.Functions);
            return functions;
        }
        
        /// <summary>
        /// Find all active modules inside a specified shader.
        /// </summary>
        /// <param name="shader">Modular shader to check</param>
        /// <param name="activeEnablers">Dictionary of active Property Enablers</param>
        /// <returns>A list of active <see cref="ShaderModule"/> inside this shader</returns>
        public static List<ShaderModule> FindActiveModules(ModularShader shader, Dictionary<string, int> activeEnablers)
        {
            List<ShaderModule> modules = new List<ShaderModule>();
            if (shader == null) return modules;

            foreach (var module in shader.BaseModules)
            {
                if (module == null) continue;
                bool hasEnabler = module.EnableProperties.Any(x => x != null && !string.IsNullOrEmpty(x.Name));
                bool hasKey = hasEnabler && module.EnableProperties.Any(x => activeEnablers.TryGetValue(x.Name, out _));
                if (!hasEnabler || !hasKey || (module.EnableProperties.All(x =>
                    {
                        if (x.Name == null || string.IsNullOrEmpty(x.Name)) return true;
                        if (!activeEnablers.TryGetValue(x.Name, out int value)) return true;
                        return x.EnableValue == value;
                    })))
                    modules.Add(module);
            }

            foreach (var module in shader.AdditionalModules)
            {
                if (module == null) continue;
                bool hasEnabler = module.EnableProperties.Any(x => x != null && !string.IsNullOrEmpty(x.Name));
                bool hasKey = hasEnabler && module.EnableProperties.Any(x => activeEnablers.TryGetValue(x.Name, out _));
                if (!hasEnabler || !hasKey || (module.EnableProperties.All(x =>
                    {
                        if (x.Name == null || string.IsNullOrEmpty(x.Name)) return true;
                        if (!activeEnablers.TryGetValue(x.Name, out int value)) return true;
                        return x.EnableValue == value;
                    })))
                    modules.Add(module);
            }

            return modules;
        }

        /// <summary>
        /// Checks for issues with the modular shader in its current state
        /// </summary>
        /// <remarks>
        /// When you're making your own automatic generation system for your application, be sure to call this function before calling <see cref="GenerateShader(string,VRLabs.ModularShaderSystem.ModularShader,bool)"/>
        /// or <see cref="GenerateMinimalShader(string,VRLabs.ModularShaderSystem.ModularShader,System.Collections.Generic.IEnumerable{UnityEngine.Material})"/>
        /// and check for errors to be sure that there won't be issues with the generation of the shader file.
        /// </remarks>
        /// <param name="shader">Shader to check</param>
        /// <returns>A list of strings detailing all errors, or an empty list if there are no issues</returns>
        public static List<string> CheckShaderIssues(ModularShader shader)
        {
            List<string> errors = new List<string>();
            var modules = FindAllModules(shader);

            for (int i = 0; i < modules.Count; i++)
            {
                var dependencies = new List<string>(modules[i].ModuleDependencies);
                for (int j = 0; j < modules.Count; j++)
                {
                    if (modules[j].IncompatibleWith.Any(x => x.Equals(modules[i].Id))) 
                        errors.Add($"Module \"{modules[j].Name}\" is incompatible with module \"{modules[i].name}\".");
                    
                    if (i != j && modules[i].Id.Equals(modules[j].Id))
                        errors.Add($"Module \"{modules[i].Name}\" is duplicate.");
                    
                    if (dependencies.Contains(modules[j].Id))
                        dependencies.Remove(modules[j].Id);
                }
                foreach (string t in dependencies)
                    errors.Add($"Module \"{modules[i].Name}\" has missing dependency id \"{t}\".");
            }
            return errors;
        }
        
        /// <summary>
        /// Checks for issues with the given list of modules
        /// </summary>
        /// <remarks>
        /// When you're making your own automatic generation system for your application, be sure to call this function before calling <see cref="GenerateShader(string,VRLabs.ModularShaderSystem.ModularShader,bool)"/>
        /// or <see cref="GenerateMinimalShader(string,VRLabs.ModularShaderSystem.ModularShader,System.Collections.Generic.IEnumerable{UnityEngine.Material})"/>
        /// and check for errors to be sure that there won't be issues with the generation of the shader file. 
        /// </remarks>
        /// <param name="modules">Modules to check</param>
        /// <returns>A list of strings detailing all errors, or an empty list if there are no issues</returns>
        public static List<string> CheckShaderIssues(List<ShaderModule> modules)
        {
            List<string> errors = new List<string>();

            for (int i = 0; i < modules.Count; i++)
            {
                var dependencies = new List<string>(modules[i].ModuleDependencies);
                for (int j = 0; j < modules.Count; j++)
                {
                    if (modules[j].IncompatibleWith.Any(x => x.Equals(modules[i].Id))) 
                        errors.Add($"Module \"{modules[j].Name}\" is incompatible with module \"{modules[i].name}\".");
                    
                    if (i != j && modules[i].Id.Equals(modules[j].Id))
                        errors.Add($"Module \"{modules[i].Name}\" is duplicate.");
                    
                    if (dependencies.Contains(modules[j].Id))
                        dependencies.Remove(modules[j].Id);
                }
                foreach (string t in dependencies)
                    errors.Add($"Module \"{modules[i].Name}\" has missing dependency id \"{t}\".");
            }
            return errors;
        }
    }
}