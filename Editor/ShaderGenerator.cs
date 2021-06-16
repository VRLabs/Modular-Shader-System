using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace VRLabs.ModularShaderSystem
{
    public class ShaderGenerator
    {
        public const string DEFAULT_VARIABLES_SINK = "VARIABLESINK";
        public const string DEFAULT_CODE_SINK = "CODESINK";

        private ModularShader _shader;
        private List<ShaderModule> _modules;
        private List<EnableProperty> _nonCgPropertyEnablers;
        private List<string> _nonCgPropertyNames;
        private List<Property> _properties;

        public void GenerateMainShader(string path, ModularShader shader)
        {
            _shader = shader;
            var shaderFile = new StringBuilder();
            _modules = FindAllModules(_shader);
            _nonCgPropertyEnablers = _modules
                .Where(x => x != null && x.Templates?.Count(y => !y.IsCGOnly) > 0 && (x.Enabled != null && !string.IsNullOrWhiteSpace(x.Enabled.Name)))
                .Select(x => x.Enabled).ToList();
            _nonCgPropertyNames = _nonCgPropertyEnablers.Select(x => x.Name).Distinct().OrderBy(x => x).ToList();
            _properties = FindAllProperties(_shader);

            WriteProperties(shaderFile);
            
            int currentlyIteratedObject = 0;

            EnablePropertyValue[] currentSettings = new EnablePropertyValue[_nonCgPropertyNames.Count];

            var variants = GenerateVariantsRecursive(shaderFile, currentlyIteratedObject, currentSettings);

            foreach ((string variantCode, StringBuilder shaderVariant) in variants)
            {
                 
                StringBuilder finalFile = CleanupShaderFile(shaderVariant);
                File.WriteAllText($"{path}/{_shader.Name}{variantCode}.shader", finalFile.ToString());
            }

            AssetDatabase.Refresh();
            
            _shader = null;
            _modules = null;
            _nonCgPropertyEnablers = null;
            _nonCgPropertyNames = null;
            _properties = null;
        }

        public void GenerateOptimizedShader(ModularShader shader, List<EnablePropertyValue> enableProperties)
        {
            _shader = shader;
            var shaderFile = new StringBuilder();
            _modules = FindAllModules(_shader);
            var PropertyEnablers = _modules
                .Where(x => x.Enabled != null && !string.IsNullOrWhiteSpace(x.Enabled.Name))
                .Select(x => x.Enabled).ToList();

            _properties = FindUsedProperties(_shader, enableProperties);

            var variantEnabledModules = _modules.Where(x => x.Enabled == null || string.IsNullOrWhiteSpace(x.Enabled.Name) || enableProperties.Select(y => y.Name).Contains(x.Enabled.Name));

            string suffix = string.Join("", enableProperties.Select(x => x.Value));

            shaderFile.PrependLineTabbed(0, "{");
            shaderFile.PrependLineTabbed(0, $"Shader \".hidden/opt/{_shader.ShaderPath}{suffix}\"");

            WriteProperties(shaderFile);

            // Write shader skeleton from templates 
            WriteShaderSkeleton(shaderFile, enableProperties);

            var functions = new List<ShaderFunction>();
            foreach (var module in variantEnabledModules)
                functions.AddRange(module.Functions);

            // Write module variables
            WriteShaderVariables(shaderFile, functions);

            // Write Functions to shader
            WriteShaderFunctions(shaderFile, functions);

            shaderFile.AppendLineTabbed(0, "}");

            File.WriteAllText($"{_shader.Name}{suffix}.shader", shaderFile.ToString());

            _modules = null;
            _properties = null;
        }

        private List<(string, StringBuilder)> GenerateVariantsRecursive(StringBuilder shaderFile, int currentlyIteratedObject, EnablePropertyValue[] currentSettings)
        {
            var files = new List<(string, StringBuilder)>();

            //In case this is the end of the tree call it will generate the finalized variant
            if (currentlyIteratedObject >= _nonCgPropertyNames.Count)
            {
                var variantShader = new StringBuilder(shaderFile.ToString());
                files.Add(GenerateShaderVariant(variantShader, currentSettings));
                return files;
            }

            //Searches all possible values for the scurrent property enabler
            List<int> possibleValues = _nonCgPropertyEnablers.Where(x => x.Name.Equals(_nonCgPropertyNames[currentlyIteratedObject]))
                .Select(x => x.EnableValue)
                .Append(0)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            //Recursively calls this function once for every possible branch referencing the next property to branch into.
            // Then aggregates all the resulting variants from this branch of the tree call, to then return it to its root
            foreach (int value in possibleValues)
            {
                var newSettings = new EnablePropertyValue[_nonCgPropertyNames.Count];
                Array.Copy(currentSettings, newSettings, _nonCgPropertyNames.Count);
                newSettings[currentlyIteratedObject] = new EnablePropertyValue { Name =_nonCgPropertyNames[currentlyIteratedObject], Value = value };
                List<(string, StringBuilder)> returnFiles = GenerateVariantsRecursive(shaderFile, currentlyIteratedObject + 1, newSettings);

                files.AddRange(returnFiles);
            }

            return files;
        }

        private (string, StringBuilder) GenerateShaderVariant(StringBuilder shaderFile, EnablePropertyValue[] currentSettings)
        {
            string suffix = string.Join("", currentSettings.Select(x => x.Value));

            var variantEnabledModules = _modules
                .Where(x => x != null)
                .Where(x => x.Enabled == null || string.IsNullOrWhiteSpace(x.Enabled.Name) || currentSettings.Select(y => y.Name).Contains(x.Enabled.Name));

            // Add Shader Location
            shaderFile.PrependLineTabbed(0, "{");
            shaderFile.PrependLineTabbed(0, $"Shader \"Hidden/{_shader.ShaderPath}{suffix}\"");

            // Write shader skeleton from templates 
            WriteShaderSkeleton(shaderFile, currentSettings);

            // Get functions currently used in this variant.
            var functions = new List<ShaderFunction>();
            foreach (var module in variantEnabledModules)
                functions.AddRange(module.Functions);

            // Write module variables
            WriteShaderVariables(shaderFile, functions);

            // Write Functions to shader
            WriteShaderFunctions(shaderFile, functions);

            if(!string.IsNullOrWhiteSpace(_shader.CustomEditor))
                shaderFile.AppendLineTabbed(1, $"CustomEditor \"{_shader.CustomEditor}\"");
            shaderFile.AppendLineTabbed(0, "}");

            
            MatchCollection m = Regex.Matches(shaderFile.ToString(), @"#K#.*$", RegexOptions.Multiline);
            foreach (Match match in m)
                shaderFile.Replace(match.Value, "");

            shaderFile.Replace("\r\n", "\n");
            return (suffix, shaderFile);
        }

        private static void WriteShaderVariables(StringBuilder shaderFile, List<ShaderFunction> functions)
        {
            WriteVariablesToSink(shaderFile, functions, DEFAULT_VARIABLES_SINK, true);
            foreach (var sink in functions.Select(x => x.VariableSinkKeyword).Distinct().Where(x => !string.IsNullOrEmpty(x) && !x.Equals(DEFAULT_VARIABLES_SINK)))
                WriteVariablesToSink(shaderFile, functions, sink);
        }

        private static void WriteVariablesToSink(StringBuilder shaderFile, List<ShaderFunction> functions, string sink, bool isDefaultSink = false)
        {
            var variablesDeclaration = new StringBuilder();
            foreach (var variable in functions
                .Where(x => x.VariableSinkKeyword.Equals(sink) || (isDefaultSink && string.IsNullOrWhiteSpace(x.VariableSinkKeyword)))
                .SelectMany(x => x.UsedVariables)
                .Distinct()
                .OrderBy(x => x.Type))
            {
                if (variable.Type.Equals("sampler2D") || variable.Type.Equals("Texture2D"))
                    variablesDeclaration.AppendLineTabbed(2, $"{variable.Type} {variable.Name}; float4 {variable.Name}_ST;");
                else
                    variablesDeclaration.AppendLineTabbed(2, $"{variable.Type} {variable.Name};");
            }

            variablesDeclaration.AppendLineTabbed(2, "#K#" + sink);
            shaderFile.Replace("#K#" + sink, variablesDeclaration.ToString());
        }

        private void WriteShaderFunctions(StringBuilder shaderFile, List<ShaderFunction> functions)
        {
            foreach (var function in functions.Where(x => x.AppendAfter.StartsWith("#K#")))
            {
                if (!shaderFile.Contains(function.AppendAfter)) continue;

                ShaderModule module = _modules.Find(x => x.Functions.Contains(function));
                StringBuilder functionCode = new StringBuilder();
                StringBuilder functionCallSequence = new StringBuilder();
                int tabs = 2;
                tabs = WriteFunctionCallSequence(functions, function, module, functionCode, functionCallSequence, tabs);
                string codeSink = string.IsNullOrWhiteSpace(function.CodeSinkKeyword) ? DEFAULT_CODE_SINK : function.CodeSinkKeyword;

                functionCode.AppendLineTabbed(tabs, "#K#" + codeSink);
                shaderFile.Replace("#K#" + codeSink, functionCode.ToString());

                functionCallSequence.AppendLineTabbed(tabs, function.AppendAfter);
                shaderFile.Replace(function.AppendAfter, functionCallSequence.ToString());
            }
        }

        private static int WriteFunctionCallSequence(List<ShaderFunction> functions, ShaderFunction function, ShaderModule module, 
            StringBuilder functionCode, StringBuilder functionCallSequence, int tabs)
        {
            functionCode.AppendMultilineTabbed(tabs, function.ShaderFunctionCode.Template);

            if (module.Enabled != null && !string.IsNullOrWhiteSpace(module.Enabled.Name))
            {
                functionCallSequence.AppendLineTabbed(tabs, $"if({module.Enabled.Name} == {module.Enabled.EnableValue})");
                functionCallSequence.AppendLineTabbed(tabs, "{");
                tabs++;
                functionCallSequence.AppendLineTabbed(tabs, $"{function.Name}();");
                foreach (var fn in functions.Where(x => x.AppendAfter.Equals(function.Name)).OrderBy(x => x.Priority))
                    WriteFunctionCallSequence(functions, fn, module, functionCode, functionCallSequence, tabs);

                functionCallSequence.AppendLineTabbed(tabs, "}");
            }
            else
            {
                functionCallSequence.AppendLineTabbed(tabs, $"{function.Name}();");
                foreach (var fn in functions.Where(x => x.AppendAfter.Equals(function.Name)).OrderBy(x => x.Priority))
                    WriteFunctionCallSequence(functions, fn, module, functionCode, functionCallSequence, tabs);
            }

            return tabs;
        }

        private void WriteShaderSkeleton(StringBuilder shaderFile, IEnumerable<EnablePropertyValue> currentSettings)
        {
            shaderFile.AppendLineTabbed(1, "SubShader");
            shaderFile.AppendLineTabbed(1, "{");

            using (var sr = new StringReader(_shader.ShaderTemplate.Template))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                    shaderFile.AppendLineTabbed(2, line);
            }

            WriteModuleTemplates(shaderFile, currentSettings);
        }

        private static List<ShaderModule> FindAllModules(ModularShader shader)
        {
            List<ShaderModule> modules = new List<ShaderModule>();
            modules.AddRange(shader.BaseModules);
            modules.AddRange(shader.AdditionalModules);
            return modules;
        }

        private static List<ShaderFunction> FindAllFunctions(ModularShader shader)
        {
            var functions = new List<ShaderFunction>();
            foreach (var module in shader.BaseModules)
                functions.AddRange(module.Functions);

            foreach (var module in shader.AdditionalModules)
                functions.AddRange(module.Functions);
            return functions;
        }

        private static List<ModuleTemplate> FindAllTemplates(ModularShader shader)
        {
            var templates = new List<ModuleTemplate>();
            foreach (var module in shader.BaseModules)
                templates.AddRange(module.Templates);

            foreach (var module in shader.AdditionalModules)
                templates.AddRange(module.Templates);
            return templates;
        }

        private static List<Property> FindAllProperties(ModularShader shader)
        {
            List<Property> properties = new List<Property>();

            properties.AddRange(shader.Properties);

            foreach (var module in shader.BaseModules.Where(x => x != null))
            {
                properties.AddRange(module.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Name)));
                if(!string.IsNullOrWhiteSpace(module.Enabled.Name))
                    properties.Add(module.Enabled);
            }

            foreach (var module in shader.AdditionalModules.Where(x => x != null))
            {
                properties.AddRange(module.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Name)));
                if(!string.IsNullOrWhiteSpace(module.Enabled.Name))
                    properties.Add(module.Enabled);
            }

            return properties.Distinct().ToList();
        }

        private static List<Property> FindUsedProperties(ModularShader shader, IEnumerable<EnablePropertyValue> values)
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
        
        private void WriteModuleTemplates(StringBuilder shaderFile, IEnumerable<EnablePropertyValue> currentSettings)
        {
            foreach (var module in _modules.Where(x => x != null && currentSettings.Select(y => y.Name).Contains(x.Enabled.Name)))
            {
                foreach (var template in module.Templates)
                {
                    var tmp = new StringBuilder();

                    tmp.AppendLineTabbed(2, "#K#" + template.Keyword);
                    shaderFile.Replace("#K#" + template.Keyword, tmp.ToString());
                }
            }

            shaderFile.AppendLineTabbed(1, "}");
        }

        private void WriteProperties(StringBuilder shaderFile)
        {
            shaderFile.AppendLineTabbed(1, "Properties");
            shaderFile.AppendLineTabbed(1, "{");

            foreach (var prop in _properties)
            {
                shaderFile.AppendLineTabbed(2, $"{prop.Attributes} {prop.Name}(\"{prop.DisplayName}\", {prop.Type}) = {prop.DefaultValue}");
            }
            shaderFile.AppendLineTabbed(1, "}");
        }
        
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