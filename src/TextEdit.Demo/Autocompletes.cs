using ImGuiNET;
using Newtonsoft.Json.Linq;
using System.Numerics;
using Veldrid;

unsafe public class TextFitersTest
{
    string[] _lines = { "aaa1.c", "bbb1.c", "ccc1.c", "aaa2.cpp", "bbb2.cpp", "ccc2.cpp", "abc.h", "hello, world" };
    ImGuiTextFilterPtr _filter;

    public TextFitersTest()
    {
        var filterPtr = ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null);
        _filter = new ImGuiTextFilterPtr(filterPtr);
    }

    public void Draw()
    {
        ImGui.Begin("Here We Go");

        _filter.Draw("This is a text filter");

        foreach (var line in _lines)
        {
            if (_filter.PassFilter(line))
                ImGui.BulletText(line);
        }

        ImGui.End();
    }

    public void Destroy()
    {
        ImGuiNative.ImGuiTextFilter_destroy(_filter.NativePtr);
    }
}

public class DropdownBoxUtility
{
    private static string input = string.Empty;
    private static List<string> filteredItems = new List<string>();
    private static string[] listNames = {
            "Pseudo",
            "Explicit",
            "Implicit",
            "Fractured",
            "Enchant",
            "Scourge",
            "Crafted",
            "Crucible",
            "Veiled",
            "Monster",
            "Delve",
            "Ultimatum",
        };
    private static int selectedListIndex = 0;
    private static JObject jsonData; // JObject to store the parsed JSON data

    public static void DrawDropdownBox()
    {
        bool isInputTextEnterPressed = ImGui.InputText("##input", ref input, 32, ImGuiInputTextFlags.EnterReturnsTrue);
        var min = ImGui.GetItemRectMin();
        var size = ImGui.GetItemRectSize();
        bool isInputTextActivated = ImGui.IsItemActivated();

        if (isInputTextActivated)
        {
            ImGui.SetNextWindowPos(new Vector2(min.X, min.Y));
            ImGui.OpenPopup("##popup");
        }

        if (ImGui.BeginPopup("##popup", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            if (isInputTextActivated)
                ImGui.SetKeyboardFocusHere(0);
            ImGui.InputText("##input_popup", ref input, 32);
            ImGui.SameLine();
            ImGui.Combo("##listCombo", ref selectedListIndex, listNames, listNames.Length);
            filteredItems.Clear();
            // Select items based on the selected list index
            string[] selectedItems = GetSelectedListItems(selectedListIndex);

            if (string.IsNullOrEmpty(input))
                foreach (string item in selectedItems)
                    filteredItems.Add(item);
            else
            {
                var parts = input.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (string str in selectedItems)
                {
                    bool allPartsMatch = true;
                    foreach (string part in parts)
                    {
                        if (!str.Contains(part, StringComparison.OrdinalIgnoreCase))
                        {
                            allPartsMatch = false;
                            break;
                        }
                    }
                    if (allPartsMatch)
                        filteredItems.Add(str);
                }
            }
            ImGui.BeginChild("scrolling_region", new Vector2(size.X * 2, size.Y * 10), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
            foreach (string item in filteredItems)
            {
                if (ImGui.Selectable(item))
                {
                    input = item;
                    ImGui.CloseCurrentPopup();
                    break;
                }
            }
            ImGui.EndChild();

            if (isInputTextEnterPressed || ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private static string[] GetSelectedListItems(int index)
    {
        if (jsonData == null)
            return new[] { "foobar", "foo", "fake", "zar" };
     
        return jsonData["result"][index]["entries"]
            .Select(entry => entry["text"]?.ToString())
            .ToArray();
    }

    private static void LoadJsonData()
    {
        // Load the JSON file into a string
        string jsonString = File.ReadAllText("stats.json");

        // Parse the JSON data into a JObject
        jsonData = JObject.Parse(jsonString);
    }
}

public static class InputWithTypeAheadSearch
{
    public static bool Draw(string id, ref string text, IList<string> items)
    {
        var inputId = ImGui.GetID(id);
        var isSearchResultWindowOpen = inputId == _activeInputId;

        if (isSearchResultWindowOpen)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.DownArrow, true))
            {
                if (_lastTypeAheadResults.Count > 0)
                {
                    _selectedResultIndex++;
                    _selectedResultIndex %= _lastTypeAheadResults.Count;
                }
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow, true))
            {
                if (_lastTypeAheadResults.Count > 0)
                {
                    _selectedResultIndex--;
                    if (_selectedResultIndex < 0)
                        _selectedResultIndex = _lastTypeAheadResults.Count - 1;
                }
            }
        }

        var wasChanged = ImGui.InputText(id, ref text, 256);

        if (ImGui.IsItemActivated())
        {
            _lastTypeAheadResults.Clear();
            _selectedResultIndex = -1;
            // THelpers.DisableImGuiKeyboardNavigation();
        }

        var isItemDeactivated = ImGui.IsItemDeactivated();

        // We defer exit to get clicks on opened popup list
        var lostFocus = isItemDeactivated || ImGui.IsKeyDown(ImGuiKey.Escape);

        if (ImGui.IsItemActive() || isSearchResultWindowOpen)
        {
            _activeInputId = inputId;

            ImGui.SetNextWindowPos(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y));
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetItemRectSize().X, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7, items.Count *  7));
            if (ImGui.Begin("##typeAheadSearchPopup", ref isSearchResultWindowOpen,
                            ImGuiWindowFlags.NoTitleBar
                            | ImGuiWindowFlags.NoMove
                            | ImGuiWindowFlags.NoResize
                            | ImGuiWindowFlags.Tooltip
                            | ImGuiWindowFlags.NoFocusOnAppearing
                            | ImGuiWindowFlags.ChildWindow
                           ))
            {
                _lastTypeAheadResults.Clear();
                int index = 0;
                // ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 1234);
                foreach (var word in items)
                {
                    if (word != null && word != text && word.Contains(text))
                    {
                        var isSelected = index == _selectedResultIndex;
                        ImGui.Selectable(word, isSelected);

                        if (ImGui.IsItemClicked() || (isSelected && ImGui.IsKeyPressed(ImGuiKey.Enter)))
                        {
                            text = word;
                            wasChanged = true;
                            _activeInputId = 0;
                            isSearchResultWindowOpen = false;
                        }

                        _lastTypeAheadResults.Add(word);
                        if (++index > 30)
                            break;
                    }
                }

                _lastCount = _lastTypeAheadResults.Count;

                ImGui.PopStyleColor();
            }

            ImGui.End();
            ImGui.PopStyleVar();
        }

        if (lostFocus)
        {
            // THelpers.RestoreImGuiKeyboardNavigation();
            _activeInputId = 0;
            isSearchResultWindowOpen = false;
        }

        return wasChanged;
    }

    private static readonly List<string> _lastTypeAheadResults = new();
    private static int _selectedResultIndex = 0;
    private static uint _activeInputId;
    private static int _lastCount;
    //private static bool _isSearchResultWindowOpen;
}