﻿using System.Globalization;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Notifications;

namespace SubmarineTracker.Windows;

// From https://github.com/Flix01/imgui/blob/c2dd0c9d58fdd6f6e6d3cad58d8e0e80ca9aebf0/addons/imguidatechooser/imguidatechooser.cpp
public static class DateWidget
{
    private static readonly Vector4 Transparent = new(1,1,1,0);

    private static readonly string[] DayNames = {"Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"};
    private static readonly string[] MonthNames = {"January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"};
    private static readonly int[] NumDaysPerMonth = {31,28,31,30,31,30,31,31,30,31,30,31};
    private const int HeightInItems = 1 + 1 + 1 + 4;

    private static readonly int MaxMonthWidthIndex = -1;
    private static uint LastOpenComboID;

    static DateWidget()
    {
        if (MaxMonthWidthIndex == -1) {
            float maxMonthWidth = 0;
            for (var i = 0; i < 12; i++)   {
                var mw = ImGui.CalcTextSize(MonthNames[i]).X;
                if (maxMonthWidth < mw) {
                    maxMonthWidth = mw;
                    MaxMonthWidthIndex = i;
                }
            }
        }
    }

    public static bool Validate(DateTime minimal, ref DateTime currentMin, ref DateTime currentMax)
    {
        var needsRefresh = false;
        if (minimal > currentMin)
        {
            currentMin = minimal;
            Plugin.PluginInterface.UiBuilder.AddNotification("Selected date can not be any earlier", "[Submarine Tracker]", NotificationType.Warning);
            needsRefresh = true;
        }
        else if (currentMin > currentMax)
        {
            currentMax = currentMin;
            needsRefresh = true;
        }

        return needsRefresh;
    }

    public static void DatePickerWithInput(string label, int id, ref string dateString, ref DateTime date, string format, bool sameLine = false, bool closeWhenMouseLeavesIt = true)
    {
        if (sameLine)
            ImGui.SameLine();

        ImGui.SetNextItemWidth(80.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputTextWithHint($"##{label}Input", format.ToUpper(), ref dateString, 32, ImGuiInputTextFlags.CallbackCompletion))
        {
            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tmp))
                date = tmp;
        }
        ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);

        ImGuiComponents.IconButton(id, FontAwesomeIcon.Calendar);
        if (DatePicker(label, ref date, closeWhenMouseLeavesIt))
            dateString = date.ToString(format);
    }

    public static bool DatePicker(string label, ref DateTime dateOut, bool closeWhenMouseLeavesIt, string leftArrow = "", string rightArrow = "")
    {
        var id = ImGui.GetID(label);
        var style = ImGui.GetStyle();

        var arrowLeft = leftArrow.Length > 0 ? leftArrow : "<";
        var arrowRight = rightArrow.Length > 0 ? rightArrow : ">";
        var arrowLeftWidth  = ImGui.CalcTextSize(arrowLeft).X;
        var arrowRightWidth = ImGui.CalcTextSize(arrowRight).X;

        var labelSize = ImGui.CalcTextSize(label, 0, true);

        var requiredMonthWidth = ImGui.CalcTextSize(MonthNames[MaxMonthWidthIndex]).X;
        var widthRequiredByCalendar = (2.0f * arrowLeftWidth) + (2.0f * arrowRightWidth) + requiredMonthWidth + ImGui.CalcTextSize("9999").X + (120.0f * ImGuiHelpers.GlobalScale);
        var popupHeight = ((labelSize.Y + (2 * style.ItemSpacing.Y)) * HeightInItems) + (style.FramePadding.Y * 3);

        var valueChanged = false;
        ImGui.SetNextWindowSize(new Vector2(widthRequiredByCalendar,widthRequiredByCalendar));
        ImGui.SetNextWindowSizeConstraints(new Vector2(widthRequiredByCalendar,popupHeight + 40), new Vector2(widthRequiredByCalendar,popupHeight + 40));

        if (!ImGui.BeginPopupContextItem(label, ImGuiPopupFlags.None))
            return valueChanged;

        if (ImGui.GetIO().MouseClicked[1])   {
            // reset date when user right clicks the date chooser header when the dialog is open
            dateOut = DateTime.Now;
        }
        else if (LastOpenComboID != id) {
            LastOpenComboID = id;
            if (dateOut.Year == 1)
                dateOut = DateTime.Now;
        }

        ImGui.PushFont(UiBuilder.MonoFont);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, style.FramePadding);
        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.Button, Transparent);

        var yearString = $"{dateOut.Year}";
        var yearPartWidth = arrowLeftWidth + arrowRightWidth + ImGui.CalcTextSize(yearString).X;

        var oldWindowRounding = style.WindowRounding;
        style.WindowRounding = 0;

        ImGui.PushID(1234);
        if (ImGui.SmallButton(arrowLeft))
            dateOut = dateOut.AddMonths(-1);
        ImGui.SameLine();

        ImGui.TextUnformatted($"{Center(MonthNames[dateOut.Month - 1], 9)}");

        ImGui.SameLine();
        if (ImGui.SmallButton(arrowRight))
            dateOut = dateOut.AddMonths(1);
        ImGui.PopID();

        ImGui.SameLine(ImGui.GetWindowWidth() - yearPartWidth - style.WindowPadding.X - style.ItemSpacing.X * 4.0f);

        ImGui.PushID(1235);
        if (ImGui.SmallButton(arrowLeft))
            dateOut = dateOut.AddYears(-1);
        ImGui.SameLine();

        ImGui.Text($"{dateOut.Year}");

        ImGui.SameLine();
        if (ImGui.SmallButton(arrowRight))
            dateOut = dateOut.AddYears(1);
        ImGui.PopID();

        ImGui.Spacing();

        var maxDayOfCurMonth = NumDaysPerMonth[dateOut.Month - 1];   // This could be calculated only when needed (but I guess it's fast in any case...)
        if (maxDayOfCurMonth == 28)   {
            var year = dateOut.Year;
            var bis = ((year % 4) == 0) && ((year % 100)!=0 || (year % 400) == 0);
            if (bis)
                maxDayOfCurMonth = 29;
        }

        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGuiColors.DalamudOrange);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGuiColors.DalamudYellow);

        ImGui.Separator();

        // Display items
        var dayOfWeek = (int) new DateTime(dateOut.Year, dateOut.Month, 1).DayOfWeek;
        for (var dw = 0; dw < 7; dw++)
        {
            ImGui.BeginGroup();
            if (dw == 0)
            {
                var textColor = ImGuiColors.DalamudGrey;
                var l = (textColor.X + textColor.Y + textColor.Z) * 0.33334f;
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(l*2.0f > 1 ? 1 : l * 2.0f,l * .5f,l * .5f, textColor.W));
            }

            ImGui.Text($"{(dw == 0 ? "" : " ")}{DayNames[dw]}");
            if (dw == 0)
                ImGui.Separator();
            else
                ImGui.Spacing();

            var curDay = dw - dayOfWeek;      // Use dayOfWeek for spacing
            for (var row = 0; row < 7; row++)
            {
                var cday = curDay + (7 * row);
                if (cday >= 0 && cday < maxDayOfCurMonth)
                {
                    ImGui.PushID(row * 10 + dw);
                    if (ImGui.SmallButton(string.Format(cday < 9 ? " {0}" : "{0}", cday + 1))) {
                        valueChanged = true;
                        ImGui.SetItemDefaultFocus();
                        dateOut = new DateTime(dateOut.Year, dateOut.Month, cday + 1);
                    }
                    ImGui.PopID();
                }
                else
                {
                    ImGui.TextUnformatted(" ");
                }
            }

            if (dw == 0)
            {
                ImGui.Separator();
                ImGui.PopStyleColor();
            }
            ImGui.EndGroup();

            if (dw != 6)
                ImGui.SameLine(ImGui.GetWindowWidth() - ((6 - dw) * (ImGui.GetWindowWidth() / 7.0f)));
        }

        style.WindowRounding = oldWindowRounding;
        ImGui.PopStyleColor(2);
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();

        var mustCloseCombo = valueChanged;
        if (closeWhenMouseLeavesIt && !mustCloseCombo) {
            var distance = ImGui.GetFontSize() * 1.75f; //1.3334f; //24;
            var pos = ImGui.GetWindowPos();
            pos.X -= distance;
            pos.Y -= distance;
            var size = ImGui.GetWindowSize();
            size.X += 2.0f * distance;
            size.Y += 2.0f * distance;
            var mousePos = ImGui.GetIO().MousePos;
            if (mousePos.X<pos.X || mousePos.Y < pos.Y || mousePos.X > pos.X + size.X || mousePos.Y > pos.Y + size.Y) {
                mustCloseCombo = true;
            }
        }

        ImGui.PopFont();
        // ImGui issue #273849, children keep popups from closing automatically
        if (mustCloseCombo)
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();

        return valueChanged;
    }

    public static string Center(string source, int length)
    {
        var spaces = length - source.Length;
        var padLeft = (spaces / 2) + source.Length;
        return source.PadLeft(padLeft).PadRight(length);
    }
}
