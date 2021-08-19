using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace VRLabs.ModularShaderSystem
{
    /// <summary>
    /// Response given by <see cref="ShaderGenerator.VerifyShaderModules"/>.
    /// It does not give a comprehensive list of issues the modular shader has, just the first one encountered.
    /// For a more comprehensive list of issues you should check the modular shader asset itself.
    /// </summary>
    public enum VerificationResponse
    {
        NoIssues,
        DuplicateModule,
        MissingDependencies,
        IncompatibleModules
    }
    
    /// <summary>
    /// Class Containing the main generator system.
    /// </summary>
    /// <remarks>
    /// This class contains all methods needed to generate both the full shader and the optimised shader
    /// TODO: actually add the optimised shader generation algorithm
    /// </remarks>
    public static class ShaderGenerator
    {
        private static ModularShader _shader;
        private static List<ShaderModule> _modules;
        private static List<EnableProperty> _variantPropertyEnablers;
        private static List<string> _variantEnablerNames;
        private static List<Property> _properties;

        /// <summary>
        /// Generates the main shader containing all the modules.
        /// </summary>
        /// <remarks>
        /// The system will generate a shader that contains all modules at once, if a module defines that a template in some cases should not be included based on some property value, it then creates both versions of the shader
        /// </remarks>
        /// <param name="path">Path to save the shader</param>
        /// <param name="shader">Modular Shader to generate</param>
        /// <param name="hideVariants">If the variant shaders should be not directly visible from the shader selector</param>
        public static void GenerateMainShader(string path, ModularShader shader, bool hideVariants = false)
        {
            _shader = shader;
            var shaderFile = new StringBuilder();
            _modules = FindAllModules(_shader);
            _variantPropertyEnablers = _modules
                .Where(x => x != null && x.Templates?.Count(y => y.NeedsVariant) > 0 && (x.Enabled != null && !string.IsNullOrWhiteSpace(x.Enabled.Name)))
                .Select(x => x.Enabled).ToList();
            _variantEnablerNames = _variantPropertyEnablers.Select(x => x.Name).Distinct().OrderBy(x => x).ToList();
            _properties = FindAllProperties(_shader);
            
            WriteProperties(shaderFile);
            
            int currentlyIteratedObject = 0;

            EnablePropertyValue[] currentSettings = new EnablePropertyValue[_variantEnablerNames.Count];

            var variants = GenerateVariantsRecursive(shaderFile, currentlyIteratedObject, currentSettings, hideVariants);

            foreach ((string variantCode, StringBuilder shaderVariant) in variants)
            {
                StringBuilder finalFile = CleanupShaderFile(shaderVariant);
                File.WriteAllText($"{path}/{_shader.Name}{variantCode}.shader", finalFile.ToString());
            }

            AssetDatabase.Refresh();
            
            _shader = null;
            _modules = null;
            _variantPropertyEnablers = null;
            _variantEnablerNames = null;
            _properties = null;
        }

        /// <summary>
        /// Generates the optimised shader with only the active modules based on the material (Still highly WIP)
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="enableProperties"></param>
        public static void GenerateOptimizedShader(ModularShader shader, List<EnablePropertyValue> enableProperties)
        {
            _shader = shader;
            var shaderFile = new StringBuilder();
            _modules = FindAllModules(_shader);

            _properties = FindUsedProperties(_shader, enableProperties);

            List<ShaderModule> variantEnabledModules = _modules
                .Where(x => x.Enabled == null || string.IsNullOrWhiteSpace(x.Enabled.Name) || enableProperties.Select(y => y.Name).Contains(x.Enabled.Name))
                .ToList();

            string suffix = string.Join("", enableProperties.Select(x => x.Value));

            shaderFile.PrependLine("{");
            shaderFile.PrependLine($"Shader \"Hidden/opt/{_shader.ShaderPath}{suffix}\"");

            WriteProperties(shaderFile);

            // Write shader skeleton from templates 
            WriteShaderSkeleton(shaderFile, enableProperties);

            var functions = new List<ShaderFunction>();
            foreach (var module in variantEnabledModules)
                functions.AddRange(module.Functions);

            // Write module variables
            WriteShaderVariables(shaderFile, variantEnabledModules, functions);

            // Write Functions to shader
            WriteShaderFunctions(shaderFile, functions);

            shaderFile.AppendLine("}");

            File.WriteAllText($"{_shader.Name}{suffix}.shader", shaderFile.ToString());

            _modules = null;
            _properties = null;
        }

        /// <summary>
        /// Verifies that the modular shader does not have errors that would cause issues when generating the shader file
        /// </summary>
        /// <remarks>
        /// When you're making your own automatic generation system for your application, be sure to call this function before calling <see cref="GenerateMainShader"/> or <see cref="GenerateOptimizedShader"/> to be sure that there won't
        /// be issues with the generation of the shader file.
        /// </remarks>
        /// <param name="shader">Shader to check</param>
        /// <returns>A <see cref="VerificationResponse"/> containing the result of the check</returns>
        public static VerificationResponse VerifyShaderModules(ModularShader shader)
        {
            var modules = FindAllModules(shader);
            var incompatibilities = modules.SelectMany(x => x.IncompatibleWith).Distinct().ToList();
            var dependencies = modules.SelectMany(x => x.ModuleDependencies).Distinct().ToList();

            for (int i = 0; i < modules.Count; i++)
            {
                if (incompatibilities.Any(x => x.Equals(modules[i].Id))) return VerificationResponse.IncompatibleModules;

                if (dependencies.Contains(modules[i].Id))
                    dependencies.Remove(modules[i].Id);
                
                for (int j = i + 1; j < modules.Count; j++)
                {
                    if (modules[i].Id.Equals(modules[j].Id))
                        return VerificationResponse.DuplicateModule;
                }
            }

            return dependencies.Count > 0 ? VerificationResponse.MissingDependencies : VerificationResponse.NoIssues;
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
            modules.AddRange(shader.BaseModules);
            modules.AddRange(shader.AdditionalModules);
            return modules;
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
                if(!string.IsNullOrWhiteSpace(module.Enabled.Name))
                    properties.Add(module.Enabled);
            }

            foreach (var module in shader.AdditionalModules.Where(x => x != null))
            {
                properties.AddRange(module.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Name) || x.Attributes.Count == 0));
                if(!string.IsNullOrWhiteSpace(module.Enabled.Name))
                    properties.Add(module.Enabled);
            }

            return properties.Distinct().ToList();
        }

        /// <summary>
        /// Find all properties that are currently used in this setup (still WIP)
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static List<Property> FindUsedProperties(ModularShader shader, IEnumerable<EnablePropertyValue> values)
        {
            List<Property> properties = new List<Property>();

            properties.AddRange(shader.Properties);

            foreach (var module in shader.BaseModules.Where(x => x.Enabled == null || string.IsNullOrWhiteSpace(x.Enabled.Name) || 
                values.Count(y => y.Name.Equals(x.Enabled.Name) && y.Value == x.Enabled.EnableValue) > 0))
                properties.AddRange(module.Properties);

            foreach (var module in shader.AdditionalModules.Where(x => x.Enabled == null || string.IsNullOrWhiteSpace(x.Enabled.Name) || 
                values.Count(y => y.Name.Equals(x.Enabled.Name) && y.Value == x.Enabled.EnableValue) > 0))
                properties.AddRange(module.Properties);

            return properties.Distinct().ToList();
        }

        // Recursively find and create variants based on the number of the enableProperties that need their own variant.
        private static List<(string, StringBuilder)> GenerateVariantsRecursive(StringBuilder shaderFile, int currentlyIteratedObject, EnablePropertyValue[] currentSettings, bool isVariantHidden)
        {
            var files = new List<(string, StringBuilder)>();

            //In case this is the end of the tree call it will generate the finalized variant
            if (currentlyIteratedObject >= _variantEnablerNames.Count)
            {
                var variantShader = new StringBuilder(shaderFile.ToString());
                files.Add(GenerateShaderVariant(variantShader, currentSettings, isVariantHidden));
                return files;
            }

            // Searches all possible values for the current property enabler
            List<int> possibleValues = _variantPropertyEnablers.Where(x => x.Name.Equals(_variantEnablerNames[currentlyIteratedObject]))
                .Select(x => x.EnableValue)
                .Append(0)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // Recursively calls this function once for every possible branch referencing the next property to branch into.
            // Then aggregates all the resulting variants from this branch of the tree call, to then return it to its root
            foreach (int value in possibleValues)
            {
                var newSettings = new EnablePropertyValue[_variantEnablerNames.Count];
                Array.Copy(currentSettings, newSettings, _variantEnablerNames.Count);
                newSettings[currentlyIteratedObject] = new EnablePropertyValue { Name =_variantEnablerNames[currentlyIteratedObject], Value = value };
                List<(string, StringBuilder)> returnFiles = GenerateVariantsRecursive(shaderFile, currentlyIteratedObject + 1, newSettings, isVariantHidden);

                files.AddRange(returnFiles);
            }

            return files;
        }

        // Generate a single shader variant given the current settings
        private static (string, StringBuilder) GenerateShaderVariant(StringBuilder shaderFile, EnablePropertyValue[] currentSettings, bool isVariantHidden)
        {
            string suffix = "";
            if (currentSettings.Any(x => x.Value != 0))
                suffix = string.Join("-", currentSettings.Select(x => x.Value));

            List<ShaderModule> variantEnabledModules = _modules
                .Where(x => x != null)
                .Where(x => x.Enabled == null || string.IsNullOrWhiteSpace(x.Enabled.Name) || !x.Templates.Any(y => y.NeedsVariant) || currentSettings.Any(y => x.Enabled.Name.Equals(y.Name) && x.Enabled.EnableValue == y.Value))
                .ToList();

            // Add Shader Location
            shaderFile.PrependLine("{");
            
            if(isVariantHidden)
                shaderFile.PrependLine($"Shader \"Hidden/{_shader.ShaderPath}{suffix}\"");
            else
                shaderFile.PrependLine($"Shader \"{_shader.ShaderPath}{suffix}\"");

            // Write shader skeleton from templates 
            WriteShaderSkeleton(shaderFile, currentSettings);

            // Get functions currently used in this variant.
            var functions = new List<ShaderFunction>();
            foreach (var module in variantEnabledModules)
                functions.AddRange(module.Functions);

            // Write module variables
            WriteShaderVariables(shaderFile, variantEnabledModules, functions);

            // Write Functions to shader
            WriteShaderFunctions(shaderFile, functions);

            if(!string.IsNullOrWhiteSpace(_shader.CustomEditor))
                shaderFile.AppendLine($"CustomEditor \"{_shader.CustomEditor}\"");
            shaderFile.AppendLine("}");

            
            MatchCollection m = Regex.Matches(shaderFile.ToString(), @"#K#.*$", RegexOptions.Multiline);
            for(int i = m.Count - 1; i>=0; i--)
                shaderFile.Replace(m[i].Value, "");

            shaderFile.Replace("\r\n", "\n");
            return (suffix, shaderFile);
        }

        // Write all variables to in their respective locations
        private static void WriteShaderVariables(StringBuilder shaderFile, List<ShaderModule> variantEnabledModules, List<ShaderFunction> functions)
        {
            WriteVariablesToKeyword(shaderFile, variantEnabledModules, functions, MSSConstants.DEFAULT_VARIABLES_KEYWORD, true);
            foreach (var keyword in functions.SelectMany(x => x.VariableKeywords).Distinct().Where(x => !string.IsNullOrEmpty(x) && !x.Equals(MSSConstants.DEFAULT_VARIABLES_KEYWORD)))
                WriteVariablesToKeyword(shaderFile, variantEnabledModules, functions, keyword);
        }

        // Write variables to the specified keyword
        private static void WriteVariablesToKeyword(StringBuilder shaderFile, List<ShaderModule> variantEnabledModules, List<ShaderFunction> functions, string keyword, bool isDefaultKeyword = false)
        {
            var variablesDeclaration = new StringBuilder();
            foreach (var variable in functions
                .Where(x => x.VariableKeywords.Any(y =>y.Equals(keyword)) || (isDefaultKeyword && x.VariableKeywords.Count == 0))
                .SelectMany(x => x.UsedVariables)
                .Concat(variantEnabledModules
                    .Where(x => x.Enabled != null && !string.IsNullOrWhiteSpace(x.Enabled.Name) && !x.Templates.Any(y => y.NeedsVariant))
                    .Select(x => x.Enabled.ToVariable()))
                .Distinct()
                .OrderBy(x => x.Type))
            {
                if (variable.Type.Equals("sampler2D") || variable.Type.Equals("Texture2D"))
                    variablesDeclaration.AppendLine($"{variable.Type} {variable.Name}; float4 {variable.Name}_ST;");
                else
                    variablesDeclaration.AppendLine($"{variable.Type} {variable.Name};");
            }

            MatchCollection m = Regex.Matches(shaderFile.ToString(), $@"#K#{keyword}\s", RegexOptions.Multiline);
            for(int i = m.Count - 1; i>=0; i--)
                shaderFile.Insert(m[i].Index, variablesDeclaration.ToString());
        }

        // Write functions to the shader
        private static void WriteShaderFunctions(StringBuilder shaderFile, List<ShaderFunction> functions)
        {
            foreach (var function in functions.Where(x => x.AppendAfter.StartsWith("#K#")))
            {
                if (!shaderFile.Contains(function.AppendAfter)) continue;

                StringBuilder functionCode = new StringBuilder();
                StringBuilder functionCallSequence = new StringBuilder();
                int tabs = 2;
                tabs = WriteFunctionCallSequence(functions, function, functionCode, functionCallSequence, tabs);
                foreach(var codeKeyword in function.CodeKeywords.Count == 0 ? new string[]{ MSSConstants.DEFAULT_CODE_KEYWORD } : function.CodeKeywords.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray())
                {
                    MatchCollection m = Regex.Matches(shaderFile.ToString(), $@"#K#{codeKeyword}\s", RegexOptions.Multiline);
                    for(int i = m.Count - 1; i>=0; i--)
                        shaderFile.Insert(m[i].Index, functionCode.ToString());
                }

                functionCallSequence.AppendLine(function.AppendAfter);
                shaderFile.Replace(function.AppendAfter, functionCallSequence.ToString());
            }
        }
        
        // Write a call sequence. Recursive
        private static int WriteFunctionCallSequence(List<ShaderFunction> functions, ShaderFunction function, StringBuilder functionCode, StringBuilder functionCallSequence, int tabs)
        {
            functionCode.AppendLine(function.ShaderFunctionCode.Template);

            ShaderModule module = _modules.Find(x => x.Functions.Contains(function));

            if (module.Enabled != null && !string.IsNullOrWhiteSpace(module.Enabled.Name) && !module.Templates.Any(x => x.NeedsVariant))
            {
                functionCallSequence.AppendLine($"if({module.Enabled.Name} == {module.Enabled.EnableValue})");
                functionCallSequence.AppendLine("{");
                tabs++;
                functionCallSequence.AppendLine($"{function.Name}();");
                foreach (var fn in functions.Where(x => x.AppendAfter.Equals(function.Name)).OrderBy(x => x.Priority))
                    WriteFunctionCallSequence(functions, fn, functionCode, functionCallSequence, tabs);

                functionCallSequence.AppendLine("}");
            }
            else
            {
                functionCallSequence.AppendLine($"{function.Name}();");
                foreach (var fn in functions.Where(x => x.AppendAfter.Equals(function.Name)).OrderBy(x => x.Priority))
                    WriteFunctionCallSequence(functions, fn, functionCode, functionCallSequence, tabs);
            }

            return tabs;
        }

        // Write down all templates to generate the initial keyworded skeleton of the shader
        private static void WriteShaderSkeleton(StringBuilder shaderFile, IEnumerable<EnablePropertyValue> currentSettings)
        {
            shaderFile.AppendLine("SubShader");
            shaderFile.AppendLine("{");
            
            shaderFile.AppendLine(_shader.ShaderTemplate.Template);

            WriteModuleTemplates(shaderFile, currentSettings);
            shaderFile.AppendLine("}");
        }

        private static void WriteModuleTemplates(StringBuilder shaderFile, IEnumerable<EnablePropertyValue> currentSettings)
        {
            IEnumerable<EnablePropertyValue> enablePropertyValues = currentSettings as EnablePropertyValue[] ?? currentSettings.ToArray();
            foreach (var module in _modules.Where(x => x != null /*&& (string.IsNullOrWhiteSpace(x.Enabled.Name) || currentSettings.Select(y => y.Name).Contains(x.Enabled.Name))*/))
            {
                foreach (var template in module.Templates)
                {
                    bool hasEnabler = !string.IsNullOrWhiteSpace(module.Enabled.Name);
                    bool isEnablerVariant = _variantEnablerNames.Contains(module.Enabled.Name);
                    var tmp = new StringBuilder();
                    if (!hasEnabler || (isEnablerVariant && enablePropertyValues.FirstOrDefault(x => x.Name.Equals(module.Enabled.Name)).Value == module.Enabled.EnableValue))
                    {
                        tmp.AppendLine(template.Template.ToString());
                    }
                    else if (!isEnablerVariant)
                    {
                        tmp.AppendLine($"if({module.Enabled.Name} == {module.Enabled.EnableValue})");
                        tmp.AppendLine("{");
                        tmp.AppendLine(template.Template.ToString());
                        tmp.AppendLine("}");
                    }

                    foreach (var keyword in template.Keywords.Count == 0 ? new string[] { MSSConstants.DEFAULT_CODE_KEYWORD } : template.Keywords.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray())
                    {
                        MatchCollection m = Regex.Matches(shaderFile.ToString(), $@"#K#{keyword}\s", RegexOptions.Multiline);
                        for(int i = m.Count - 1; i>=0; i--)
                            shaderFile.Insert(m[i].Index, tmp.ToString());
                        
                        m = Regex.Matches(shaderFile.ToString(), $@"#KI#{keyword}\s", RegexOptions.Multiline);
                        for(int i = m.Count - 1; i>=0; i--)
                            shaderFile.Insert(m[i].Index, tmp.ToString());
                    }
                }
                MatchCollection mki = Regex.Matches(shaderFile.ToString(), @"#KI#.*$", RegexOptions.Multiline);
                for(int i = mki.Count - 1; i>=0; i--)
                    shaderFile.Replace(mki[i].Value, "");
            }
        }

        private static void WriteProperties(StringBuilder shaderFile)
        {
            shaderFile.AppendLine("Properties");
            shaderFile.AppendLine("{");

            if (_shader.UseTemplatesForProperties)
            {
                if(_shader.ShaderPropertiesTemplate != null)
                    shaderFile.AppendLine(_shader.ShaderPropertiesTemplate.Template);
                
                shaderFile.AppendLine($"#K#{MSSConstants.TEMPLATE_PROPERTIES_KEYWORD}");
            }
            else
            {
                foreach (var prop in _properties)
                {
                    if (string.IsNullOrWhiteSpace(prop.Type) && !string.IsNullOrWhiteSpace(prop.Name))
                    {
                        prop.Type = "Float";
                        prop.DefaultValue = "0.0";
                    }

                    string attributes = prop.Attributes.Count == 0 ? "" : $"[{string.Join("][", prop.Attributes)}]";
                    shaderFile.AppendLine(string.IsNullOrWhiteSpace(prop.Name) ? attributes : $"{attributes} {prop.Name}(\"{prop.DisplayName}\", {prop.Type}) = {prop.DefaultValue}");
                }
            }

            shaderFile.AppendLine("}");
        }

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
            
            if(!string.IsNullOrWhiteSpace(ln))
                if (CheckPropertyBlockLine(builder, reader, ln, ref tabs, ref deleteEmptyLine))
                    return true;
            
            if (line.StartsWith("}") && ln != null && ln.Trim().StartsWith("SubShader"))
                return true;
            return false;
        }
        
        // Cleanup the final shader file by indenting it decently
        private static StringBuilder CleanupShaderFile(StringBuilder shaderVariant)
        {
            var finalFile = new StringBuilder(); ;
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
                        string ln= sr.ReadLine()?.Trim();               // When the previous line is the one containing "Properties" we always know 
                        finalFile.AppendLineTabbed(tabs, ln);   // that the next line is "{" so we just write it down before increasing the tabs
                        tabs++;
                        while ((ln = sr.ReadLine()) != null)    // we should be escaping this loop way before actually meeting the condition, but you never know
                        {
                            if (CheckPropertyBlockLine(finalFile, sr, ln, ref tabs, ref deleteEmptyLine))
                                break;
                        }
                        continue;
                    }

                    if (line.StartsWith("}"))
                        tabs--;
                    finalFile.AppendLineTabbed(tabs, line);
                    if (line.StartsWith("{"))
                        tabs++;
                }
            }

            return finalFile;
        }
    }
}