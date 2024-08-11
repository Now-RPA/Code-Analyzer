using CodeAnalyzer.Models.Bot;
using System.Xml;
using StringCompare = CodeAnalyzer.Models.Bot.StringCompare;

namespace CodeAnalyzer.Services.Parser
{
    public static class BotXmlParser
    {

        public static Process Parse(string xmlFilePath)
        {
            XmlDocument xmlDoc = new();
            xmlDoc.Load(xmlFilePath);
            XmlElement rootElement = xmlDoc.DocumentElement!;

            string? idValue = GetElementValue(rootElement, "ID");

            Process botProcess = new()
            {
                Id = Guid.TryParse(idValue, out var id) ? id : Guid.Empty,
                StartupActivityId = ParseGuid(GetElementValue(rootElement, "StartupActivityID")),
                UserPlugins = GetUserPlugins(rootElement),
                Plugins = GetPlugins(rootElement),
                Activities = GetActivities(rootElement),
                Variables = GetGlobalVariables(rootElement),
                Description = GetElementValue(rootElement, "Description") ?? string.Empty,
            };
            return botProcess;
        }

        private static List<SystemPlugin> GetPlugins(XmlElement parentElement)
        {
            List<SystemPlugin> systemPlugins = [];
            XmlNodeList pluginNodes = parentElement.SelectNodes("References/AutxPluginReference")!;
            foreach (XmlNode pluginNode in pluginNodes)
            {
                SystemPlugin plugin = new()
                {
                    Id = ParseGuid(GetElementValue(pluginNode, "ID")),
                    Name = GetElementValue(pluginNode, "Name") ?? string.Empty,
                    PluginId = ParseGuid(GetElementValue(pluginNode, "PluginID")),
                    Version = GetElementValue(pluginNode, "Version") ?? string.Empty,
                    Signature = GetElementValue(pluginNode, "Signature") ?? string.Empty
                };
                systemPlugins.Add(plugin);
            }
            return systemPlugins;
        }

        private static List<UserPlugin> GetUserPlugins(XmlElement parentElement)
        {
            List<UserPlugin> userPlugins = [];
            XmlNodeList userPluginNodes = parentElement.SelectNodes("UserPlugins/AutxUserPluginReference")!;
            foreach (XmlNode userPluginNode in userPluginNodes)
            {
                UserPlugin userPlugin = new()
                {
                    Id = ParseGuid(GetElementValue(userPluginNode, "ID")),
                    Name = GetElementValue(userPluginNode, "Name") ?? string.Empty,
                    PluginId = ParseGuid(GetElementValue(userPluginNode, "PluginID")),
                    Version = GetElementValue(userPluginNode, "Version") ?? string.Empty,
                    AssemblyPath = GetElementValue(userPluginNode, "AssemblyPath") ?? string.Empty
                };
                userPlugins.Add(userPlugin);
            }
            return userPlugins;
        }

        private static List<Activity> GetActivities(XmlElement parentElement)
        {
            List<Activity> activities = [];
            XmlNodeList activityNodes = parentElement.SelectNodes("Activities/AutxActivity")!;
            foreach (XmlNode activityNode in activityNodes)
            {
                Activity activity = new()
                {
                    Id = ParseGuid(GetElementValue(activityNode, "ID")),
                    ParentId = ParseGuid(GetElementValue(activityNode, "ParentID")),
                    Name = GetElementValue(activityNode, "Name") ?? string.Empty,
                    Variables = GetActivityVariables(activityNode),
                    Items = GetDesignItems(activityNode),
                    OnErrorAction = ParseEnum<OnErrorAction>(GetElementValue(activityNode, "OnErrorAction")),
                    MaxRetries = int.TryParse(GetElementValue(activityNode, "MaxRetries"), out var maxRetries) ? maxRetries : 0,
                    RetryDelay = int.TryParse(GetElementValue(activityNode, "RetryDelay"), out var retryDelay) ? retryDelay : 0,
                    OnErrorActionAfterRetry = ParseEnum<OnErrorAction>(GetElementValue(activityNode, "OnErrorActionAfterRetry")),
                    RootPath = GetElementValue(activityNode, "RootPath") ?? string.Empty
                };
                activities.Add(activity);
            }
            return activities;
        }

        private static List<AbstractDesignItem> GetDesignItems(XmlNode parentNode)
        {
            List<AbstractDesignItem> designItems = [];
            XmlNodeList itemNodes = parentNode.SelectNodes("Items/DesignItem")!;
            foreach (XmlNode itemNode in itemNodes)
            {
                string? itemType = GetAttributeValue(itemNode, "xsi:type");
                Guid id = ParseGuid(GetElementValue(itemNode, "ID"));
                Guid parentId = ParseGuid(GetElementValue(itemNode, "ParentID"));

                switch (itemType)
                {
                    case "AutxControlConnection":
                        ControlConnection controlConnection = new()
                        {
                            Id = id,
                            ParentId = parentId,
                            SourceComponentId = ParseGuid(GetElementValue(itemNode, "SourceComponentID")),
                            SourcePortId = ParseGuid(GetElementValue(itemNode, "SourcePortID")),
                            SinkComponentId = ParseGuid(GetElementValue(itemNode, "SinkComponentID")),
                            SinkPortId = ParseGuid(GetElementValue(itemNode, "SinkPortID")),
                            Type = itemType
                        };
                        designItems.Add(controlConnection);
                        break;
                    case "AutxDataConnection":
                        DataConnection dataConnection = new()
                        {
                            Id = id,
                            ParentId = parentId,
                            SourceComponentId = ParseGuid(GetElementValue(itemNode, "SourceComponentID")),
                            SourcePortId = ParseGuid(GetElementValue(itemNode, "SourcePortID")),
                            SinkComponentId = ParseGuid(GetElementValue(itemNode, "SinkComponentID")),
                            SinkPortId = ParseGuid(GetElementValue(itemNode, "SinkPortID")),
                            Type = itemType
                        };
                        designItems.Add(dataConnection);
                        break;
                    case "AutxCommentConnection":
                        CommentConnection commentConnection = new()
                        {
                            Id = id,
                            ParentId = parentId,
                            SourceComponentId = ParseGuid(GetElementValue(itemNode, "SourceComponentID")),
                            SourcePortId = ParseGuid(GetElementValue(itemNode, "SourcePortID")),
                            SinkComponentId = ParseGuid(GetElementValue(itemNode, "SinkComponentID")),
                            SinkPortId = ParseGuid(GetElementValue(itemNode, "SinkPortID")),
                            Type = itemType
                        };
                        designItems.Add(commentConnection);
                        break;
                    default:
                        bool hasControlInPort = itemNode.SelectSingleNode("ControlIn") != null;
                        bool hasControlOutPort = itemNode.SelectSingleNode("ControlOut") != null;

                        if (hasControlInPort || hasControlOutPort)
                        {
                            ControlIn? controlIn = hasControlInPort ? new ControlIn
                            {
                                Id = ParseGuid(GetElementValue(itemNode, "ControlIn/ID")),
                                Name = GetElementValue(itemNode, "ControlIn/Name") ?? string.Empty,
                                Visibility = bool.Parse(GetElementValue(itemNode, "ControlIn/Visibility") ?? "false"),
                                AllowDelete = bool.Parse(GetElementValue(itemNode, "ControlIn/AllowDelete") ?? "false")
                            } : null;

                            ControlOut? controlOut = hasControlOutPort ? new ControlOut
                            {
                                Id = ParseGuid(GetElementValue(itemNode, "ControlOut/ID")),
                                Name = GetElementValue(itemNode, "ControlOut/Name") ?? string.Empty,
                                Visibility = bool.Parse(GetElementValue(itemNode, "ControlOut/Visibility") ?? "false"),
                                AllowDelete = bool.Parse(GetElementValue(itemNode, "ControlOut/AllowDelete") ?? "false")
                            } : null;

                            List<DataTransform> dataTransforms = GetDataTransforms(itemNode);
                            List<MappedVariable> mappedVariables = GetMappedVariables(itemNode);
                            ExecutableItem executableItem = new()
                            {
                                Id = id,
                                ParentId = parentId,
                                Name = GetElementValue(itemNode, "Name") ?? string.Empty,
                                X = double.TryParse(GetElementValue(itemNode, "X"), out var x) ? x : 0,
                                Y = double.TryParse(GetElementValue(itemNode, "Y"), out var y) ? y : 0,
                                Breakpoint = bool.Parse(GetElementValue(itemNode, "BreakPoint") ?? "false"),
                                ControlIn = controlIn,
                                ControlOut = controlOut,
                                OnErrorAction = ParseEnum(GetElementValue(itemNode, "OnErrorAction"), OnErrorAction.Inherit),
                                MaxRetries = int.Parse(GetElementValue(itemNode, "MaxRetries") ?? "1"),
                                RetryDelay = int.Parse(GetElementValue(itemNode, "RetryDelay") ?? "0"),
                                OnErrorActionAfterRetry = ParseEnum(GetElementValue(itemNode, "OnErrorActionAfterRetry"), OnErrorAction.Inherit),
                                BeforeDelay = int.Parse(GetElementValue(itemNode, "BeforeDelay") ?? "0"),
                                AfterDelay = int.Parse(GetElementValue(itemNode, "AfterDelay") ?? "0"),
                                EnableTimeout = bool.Parse(GetElementValue(itemNode, "EnableTimeout") ?? "false"),
                                Timeout = int.Parse(GetElementValue(itemNode, "Timeout") ?? "60"),
                                CommentPortId = ParseGuid(GetElementValue(itemNode, "CommentConnectionPort/ID")),
                                ClassName = GetElementValue(itemNode, "ClassName"),
                                MethodName = GetElementValue(itemNode, "MethodName"),
                                ObjectId = ParseGuid(GetElementValue(itemNode, "ObjectID")),
                                ErrorOutPortId = ParseGuid(GetElementValue(itemNode, "ErrorOut/ID")),
                                ErrorMessagePortId = ParseGuid(GetElementValue(itemNode, "ErrorMessagePort/ID")),
                                LogMessage = GetElementValue(itemNode, "MessagePort/StaticValue"),
                                LogMode = GetElementValue(itemNode, "LogMode"),
                                DataTransforms = dataTransforms,
                                MappedVariables = mappedVariables,
                                Type = itemType ?? string.Empty
                            };
                            designItems.Add(executableItem);
                        }
                        else
                        {
                            GenericDesignItem genericItem = new()
                            {
                                Id = id,
                                ParentId = parentId,
                                Name = GetElementValue(itemNode, "Name") ?? string.Empty,
                                X = double.TryParse(GetElementValue(itemNode, "X"), out var x) ? x : 0,
                                Y = double.TryParse(GetElementValue(itemNode, "Y"), out var y) ? y : 0,
                                Type = itemType ?? string.Empty
                            };
                            designItems.Add(genericItem);
                        }
                        break;
                }
            }

            return designItems;
        }

        private static List<ActivityVariable> GetActivityVariables(XmlNode parentNode)
        {
            List<ActivityVariable> variables = [];
            XmlNodeList variableNodes = parentNode.SelectNodes("Variables/AutxObject")!;
            foreach (XmlNode variableNode in variableNodes)
            {
                ActivityVariable variable = new()
                {
                    Id = ParseGuid(GetElementValue(variableNode, "ID")),
                    Name = GetElementValue(variableNode, "Name") ?? string.Empty,
                    ActivityId = ParseGuid(GetElementValue(variableNode, "ActivityID")),
                    RootPath = GetElementValue(variableNode, "RootPath") ?? string.Empty,
                    DataType = GetAttributeValue(variableNode, "xsi:type") ?? string.Empty
                };
                variables.Add(variable);
            }
            return variables;
        }

        private static List<GlobalVariable> GetGlobalVariables(XmlElement parentElement)
        {
            List<GlobalVariable> variables = [];
            XmlNodeList variableNodes = parentElement.SelectNodes("Variables/AutxObject")!;
            foreach (XmlNode variableNode in variableNodes)
            {
                string variableType = GetAttributeValue(variableNode, "xsi:type") ?? string.Empty;
                GlobalVariable variable = variableType switch
                {
                    "UTL.RPA.CONNECTORS.UAC.AutxUniversalApplication" => ParseUniversalApplicationVariable(variableNode),
                    _ => new GlobalVariable
                    {
                        Id = ParseGuid(GetElementValue(variableNode, "ID")),
                        Name = GetElementValue(variableNode, "Name") ?? string.Empty,
                        RootPath = GetElementValue(variableNode, "RootPath") ?? string.Empty,
                        DataType = variableType
                    }
                };
                variables.Add(variable);
            }
            return variables;
        }

        private static UniversalAppConnector ParseUniversalApplicationVariable(XmlNode variableNode)
        {
            return new UniversalAppConnector
            {
                Id = ParseGuid(GetElementValue(variableNode, "ID")),
                Name = GetElementValue(variableNode, "Name") ?? string.Empty,
                RootPath = GetElementValue(variableNode, "RootPath") ?? string.Empty,
                DataType = GetAttributeValue(variableNode, "xsi:type") ?? string.Empty,
                ProcessId = ParseGuid(GetElementValue(variableNode, "ProcessID")),
                IsRemoteExecutionEnabled = bool.Parse(GetElementValue(variableNode, "IsRemoteExecutionEnabled") ?? "false"),
                IsolationPlatform = GetElementValue(variableNode, "IsolationPlatform") ?? string.Empty,
                IsolationSessionType = GetElementValue(variableNode, "IsolationSessionType") ?? string.Empty,
                Screens = GetScreens(variableNode)
            };
        }


        private static List<BaseScreen> GetScreens(XmlNode parentNode)
        {
            List<BaseScreen> screens = [];
            XmlNodeList screenNodes = parentNode.SelectNodes("Items/AutxObject")!;
            foreach (XmlNode screenNode in screenNodes)
            {
                string screenType = GetAttributeValue(screenNode, "xsi:type") ?? string.Empty;
                BaseScreen screen = screenType switch
                {
                    "UTL.RPA.CONNECTORS.WEB.CHROMEBROWSER.AutxWebScreen" => new ChromeConnectorScreen
                    {
                        BrowserType = GetElementValue(screenNode, "BrowserType") ?? string.Empty
                    },
                    "UTL.RPA.CONNECTORS.WINDOWS.AutxWinScreen" => new WindowsConnectorScreen(),
                    _ => new GenericScreen()
                };

                screen = screen with
                {
                    Id = ParseGuid(GetElementValue(screenNode, "ID")),
                    Name = GetElementValue(screenNode, "Name") ?? string.Empty,
                    Type = screenType,
                    RootPath = GetElementValue(screenNode, "RootPath") ?? string.Empty,
                    MatchRules = GetMatchRules(screenNode.SelectSingleNode("MatchRules")),
                    Locators = GetLocators(screenNode.SelectSingleNode("Locators")),
                    Elements = GetScreenElements(screenNode.SelectSingleNode("Items")),
                    SelectedLocator = GetSelectedLocator(screenNode.SelectSingleNode("Locator"))
                };

                screens.Add(screen);
            }
            return screens;
        }

        private static List<BaseMatchRule> GetMatchRules(XmlNode? matchRulesNode)
        {
            List<BaseMatchRule> matchRules = [];
            if (matchRulesNode == null) return matchRules;

            foreach (XmlNode ruleNode in matchRulesNode.ChildNodes)
            {
                string ruleType = GetAttributeValue(ruleNode, "xsi:type") ?? string.Empty;
                BaseMatchRule rule = ruleType switch
                {
                    "UTL.RPA.CONNECTORS.WEB.CHROMEBROWSER.TitleMatchRule" or
                    "UTL.RPA.CONNECTORS.WEB.CHROMEBROWSER.UrlMatchRule" or
                    "UTL.RPA.CONNECTORS.WINDOWS.TitleMatchRule" or
                    "UTL.RPA.CONNECTORS.WINDOWS.ClassMatchRule" or
                    "UTL.RPA.CONNECTORS.WINDOWS.NameMatchRule" or
                    "UTL.RPA.CONNECTORS.WINDOWS.PathMatchRule" => new StringComparerMatchRule
                    {
                        Comparer = GetStringComparer(ruleNode.SelectSingleNode("Comparer"))
                    },
                    "UTL.RPA.CONNECTORS.WEB.CHROMEBROWSER.IndexMatchRule" or
                    "UTL.RPA.CONNECTORS.WINDOWS.IndexMatchRule" => new IndexMatchRule
                    {
                        Index = int.Parse(GetElementValue(ruleNode, "Index") ?? "0")
                    },
                    "UTL.RPA.CONNECTORS.WINDOWS.IDMatchRule" => new ElementMatchRule
                    {
                        ElementId = GetElementValue(ruleNode, "ElementID") ?? string.Empty
                    },
                    "UTL.RPA.CONNECTORS.WINDOWS.TypeMatchRule" => new ElementMatchRule
                    {
                        ElementType = GetElementValue(ruleNode, "ElementType") ?? string.Empty
                    },
                    _ => new GenericMatchRule()
                };

                rule = rule with
                {
                    Id = ParseGuid(GetElementValue(ruleNode, "ID")),
                    Enabled = bool.Parse(GetElementValue(ruleNode, "Enabled") ?? "false"),
                    Type = ruleType
                };

                matchRules.Add(rule);
            }

            return matchRules;
        }

        private static List<Locator> GetLocators(XmlNode? locatorsNode)
        {
            List<Locator> locators = [];
            if (locatorsNode == null) return locators;

            foreach (XmlNode locatorNode in locatorsNode.ChildNodes)
            {
                Locator locator = new()
                {
                    Id = ParseGuid(GetElementValue(locatorNode, "ID")),
                    LocateBy = GetElementValue(locatorNode, "LocateBy") ?? string.Empty,
                    Value = GetElementValue(locatorNode, "Value") ?? string.Empty,
                    Selected = bool.Parse(GetElementValue(locatorNode, "Selected") ?? "false")
                };
                locators.Add(locator);
            }

            return locators;
        }

        private static List<ScreenElement> GetScreenElements(XmlNode? itemsNode)
        {
            List<ScreenElement> elements = [];
            if (itemsNode == null) return elements;

            foreach (XmlNode elementNode in itemsNode.ChildNodes)
            {
                ScreenElement element = new()
                {
                    Id = ParseGuid(GetElementValue(elementNode, "ID")),
                    Name = GetElementValue(elementNode, "Name") ?? string.Empty,
                    Type = GetAttributeValue(elementNode, "xsi:type") ?? string.Empty,
                    RootPath = GetElementValue(elementNode, "RootPath") ?? string.Empty,
                    MatchRules = GetMatchRules(elementNode.SelectSingleNode("MatchRules")),
                    JsMatchRules = GetJsMatchRules(elementNode.SelectSingleNode("JsMatchRules")),
                    Locators = GetLocators(elementNode.SelectSingleNode("Locators")),
                    SelectedLocator = GetSelectedLocator(elementNode.SelectSingleNode("Locator")),
                    MatchCriteria = GetElementValue(elementNode, "MatchCriteria") ?? string.Empty
                };
                elements.Add(element);
            }

            return elements;
        }

        private static List<JsMatchRule> GetJsMatchRules(XmlNode? jsMatchRulesNode)
        {
            List<JsMatchRule> jsMatchRules = [];
            if (jsMatchRulesNode == null) return jsMatchRules;

            foreach (XmlNode ruleNode in jsMatchRulesNode.ChildNodes)
            {
                JsMatchRule rule = new()
                {
                    Id = ParseGuid(GetElementValue(ruleNode, "ID")),
                    JId = ParseGuid(GetElementValue(ruleNode, "JID")),
                    Type = GetElementValue(ruleNode, "Type") ?? string.Empty,
                    Name = GetElementValue(ruleNode, "Name") ?? string.Empty,
                    Comparer = GetElementValue(ruleNode, "Comparer") ?? string.Empty,
                    Value = GetElementValue(ruleNode, "Value") ?? string.Empty,
                    IgnoreCase = bool.Parse(GetElementValue(ruleNode, "IgnoreCase") ?? "false"),
                    Escape = bool.Parse(GetElementValue(ruleNode, "Escape") ?? "false"),
                    Trim = bool.Parse(GetElementValue(ruleNode, "Trim") ?? "false"),
                    Enabled = bool.Parse(GetElementValue(ruleNode, "Enabled") ?? "false")
                };
                jsMatchRules.Add(rule);
            }

            return jsMatchRules;
        }

        private static Locator? GetSelectedLocator(XmlNode? selectedLocatorNode)
        {
            if (selectedLocatorNode == null) return null;

            return new Locator
            {
                Id = ParseGuid(GetElementValue(selectedLocatorNode, "ID")),
                LocateBy = GetElementValue(selectedLocatorNode, "LocateBy") ?? string.Empty,
                Value = GetElementValue(selectedLocatorNode, "Value") ?? string.Empty,
                Selected = true
            };
        }

        private static StringCompare GetStringComparer(XmlNode? comparerNode)
        {
            if (comparerNode == null) return new StringCompare();

            return new StringCompare
            {
                ComparisonValue = GetElementValue(comparerNode, "ComparisonValue") ?? string.Empty,
                Type = GetElementValue(comparerNode, "Type") ?? string.Empty
            };
        }
        private static string? GetElementValue(XmlNode? parentNode, string elementName)
        {
            return parentNode?.SelectSingleNode(elementName)?.InnerText;
        }

        private static string? GetAttributeValue(XmlNode? node, string attributeName)
        {
            return node?.Attributes?[attributeName]?.Value;
        }

        private static Guid ParseGuid(string? value)
        {
            return Guid.TryParse(value, out var guid) ? guid : Guid.Empty;
        }

        private static T ParseEnum<T>(string? value, T defaultValue = default) where T : struct, Enum
        {
            return Enum.TryParse<T>(value, out var enumValue) ? enumValue : defaultValue;
        }
        private static List<DataTransform> GetDataTransforms(XmlNode itemNode)
        {
            List<DataTransform> dataTransforms = [];
            XmlNodeList? dataTransformNodes = itemNode.SelectNodes(".//DataTransform");

            if (dataTransformNodes != null)
            {
                foreach (XmlNode dataTransformNode in dataTransformNodes)
                {
                    string attributeType = GetAttributeValue(dataTransformNode, "xsi:type") ?? string.Empty;

                    DataTransform dataTransform = new()
                    {
                        Id = ParseGuid(GetElementValue(dataTransformNode, "ID")),
                        Enabled = bool.Parse(GetElementValue(dataTransformNode, "Enabled") ?? "false"),
                        Script = GetElementValue(dataTransformNode, "Script") ?? string.Empty,
                        ScriptLanguage = GetElementValue(dataTransformNode, "ScriptLanguage") ?? string.Empty,
                        AttributeType = attributeType
                    };
                    dataTransforms.Add(dataTransform);
                }
            }

            return dataTransforms;
        }
        private static List<MappedVariable> GetMappedVariables(XmlNode itemNode)
        {
            List<MappedVariable> mappedVariables = [];
            XmlNodeList? mappedVariableNodes = itemNode.SelectNodes(".//MappedVariable");

            if (mappedVariableNodes != null)
            {
                foreach (XmlNode mappedVariableNode in mappedVariableNodes)
                {
                    DataIn? dataIn = GetDataIn(mappedVariableNode);
                    DataOut? dataOut = GetDataOut(mappedVariableNode);

                    MappedVariable mappedVariable = new()
                    {
                        Id = ParseGuid(GetElementValue(mappedVariableNode, "ID")),
                        IsGlobal = bool.Parse(GetElementValue(mappedVariableNode, "IsGlobal") ?? "false"),
                        DataIn = dataIn,
                        DataOut = dataOut
                    };

                    mappedVariables.Add(mappedVariable);
                }
            }

            return mappedVariables;
        }

        private static DataIn? GetDataIn(XmlNode mappedVariableNode)
        {
            XmlNode? dataInNode = mappedVariableNode.SelectSingleNode("DataIn");
            if (dataInNode == null)
                return null;

            return new DataIn
            {
                Id = ParseGuid(GetElementValue(dataInNode, "ID")),
                Name = GetElementValue(dataInNode, "Name") ?? string.Empty
            };
        }

        private static DataOut? GetDataOut(XmlNode mappedVariableNode)
        {
            XmlNode? dataOutNode = mappedVariableNode.SelectSingleNode("DataOut");
            if (dataOutNode == null)
                return null;

            return new DataOut
            {
                Id = ParseGuid(GetElementValue(dataOutNode, "ID")),
                Name = GetElementValue(dataOutNode, "Name") ?? string.Empty
            };
        }
    }
}