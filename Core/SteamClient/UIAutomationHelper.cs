using System;
using System.Threading.Tasks;
using System.Windows.Automation;
using SteamAutoLauncher.Core.Logging;

namespace SteamAutoLauncher.Core.SteamClient
{
    public class UIAutomationHelper
    {
        private const int SearchTimeoutMs = 10000;

        public async Task<bool> EnterSteamGuardCodeAsync(string code)
        {
            try
            {
                Logger.LogInfo($"Attempting to enter Steam Guard code via UI Automation");

                // Find the root element
                var root = AutomationElement.RootElement;
                if (root == null)
                {
                    Logger.LogError("Failed to get root UI element");
                    return false;
                }

                // Search for input fields where we can type the code
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                while (stopwatch.ElapsedMilliseconds < SearchTimeoutMs)
                {
                    // Look for password input or text input fields
                    var inputBoxes = FindInputBoxes(root);
                    
                    if (inputBoxes.Count > 0)
                    {
                        foreach (var inputBox in inputBoxes)
                        {
                            try
                            {
                                // Try to set focus and type
                                if (inputBox.TryGetCurrentPattern(ValuePattern.Pattern, out object? valuePatternObj) && 
                                    valuePatternObj is ValuePattern valuePattern)
                                {
                                    inputBox.SetFocus();
                                    await Task.Delay(100);
                                    
                                    valuePattern.SetValue(code);
                                    Logger.LogSuccess($"Successfully entered Steam Guard code");
                                    return true;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning($"Failed to enter code in input box: {ex.Message}");
                            }
                        }
                    }

                    await Task.Delay(500);
                }

                Logger.LogWarning("Steam Guard code input field not found (falling back to clipboard)");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error with UI Automation: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ClickEnterCodeInsteadAsync()
        {
            try
            {
                Logger.LogInfo("Attempting to click 'Enter a code instead' button");

                var root = AutomationElement.RootElement;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                while (stopwatch.ElapsedMilliseconds < SearchTimeoutMs)
                {
                    var buttons = FindButtonsByText(root, "Enter a code instead");
                    
                    if (buttons.Count > 0)
                    {
                        foreach (var button in buttons)
                        {
                            try
                            {
                                if (button.TryGetCurrentPattern(InvokePattern.Pattern, out object? invokePatternObj) && 
                                    invokePatternObj is InvokePattern invokePattern)
                                {
                                    button.SetFocus();
                                    await Task.Delay(100);
                                    invokePattern.Invoke();
                                    Logger.LogSuccess("Clicked 'Enter a code instead' button");
                                    return true;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning($"Failed to click button: {ex.Message}");
                            }
                        }
                    }

                    await Task.Delay(500);
                }

                Logger.LogInfo("'Enter a code instead' button not found (may not be needed)");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error clicking button: {ex.Message}");
                return false;
            }
        }

        private static System.Collections.Generic.List<AutomationElement> FindInputBoxes(AutomationElement root)
        {
            var inputs = new System.Collections.Generic.List<AutomationElement>();
            
            try
            {
                var condition = new OrCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox)
                );

                var treeWalker = TreeWalker.ContentViewWalker;
                var element = treeWalker.GetFirstChild(root);
                
                while (element != null)
                {
                    if (condition.Matches(element))
                    {
                        inputs.Add(element);
                    }

                    var child = treeWalker.GetFirstChild(element);
                    if (child != null)
                    {
                        element = child;
                    }
                    else
                    {
                        element = treeWalker.GetNextSibling(element);
                    }
                }
            }
            catch
            {
                // Ignore UI automation errors
            }

            return inputs;
        }

        private static System.Collections.Generic.List<AutomationElement> FindButtonsByText(AutomationElement root, string text)
        {
            var buttons = new System.Collections.Generic.List<AutomationElement>();

            try
            {
                var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                var allButtons = root.FindAll(TreeScope.Descendants, condition);

                foreach (AutomationElement button in allButtons)
                {
                    var buttonText = button.Current.Name;
                    if (buttonText != null && buttonText.Contains(text, StringComparison.OrdinalIgnoreCase))
                    {
                        buttons.Add(button);
                    }
                }
            }
            catch
            {
                // Ignore UI automation errors
            }

            return buttons;
        }
    }
}
