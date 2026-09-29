using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Item slot grid shared by the inventory, shop and loot windows
public static class ItemGrid
{
    public static readonly Color SlotColor = new Color(0, 0, 0, 0.1f); // Light transparent slot background
    public static readonly Color HoverColor = new Color(1, 1, 1, 0.3f); // Lighten background on hover
    public static readonly Color SelectedColor = new Color(0.5f, 1, 1, 0.3f);

    // Builds a wrapping grid with at least minSlots slots. Filled slots show the tooltip while hovered
    // (placed by moveTooltip) and report clicks with their slot and item index; isSelected keeps a
    // slot highlighted after the mouse leaves it.
    public static VisualElement Build(List<Item> items, int minSlots, ItemTooltip tooltip, Func<Item, string> goldText,
        Action<Vector2> moveTooltip, Action<VisualElement, int> onClick, Func<int, bool> isSelected = null)
    {
        VisualElement gridContainer = new VisualElement();
        gridContainer.style.flexDirection = FlexDirection.Row;
        gridContainer.style.flexWrap = Wrap.Wrap; // Allow wrapping into multiple rows
        gridContainer.style.justifyContent = Justify.FlexStart; // Align left
        gridContainer.style.alignItems = Align.Center; // Center items vertically
        gridContainer.style.paddingBottom = 10;
        gridContainer.style.width = Length.Percent(100);
        gridContainer.style.height = Length.Percent(100);

        int totalSlots = Mathf.Max(items.Count, minSlots);
        for (int i = 0; i < totalSlots; i++)
        {
            int index = i; // Captured per slot for the callbacks
            VisualElement itemSlot = new VisualElement();
            itemSlot.style.flexDirection = FlexDirection.Column;
            itemSlot.style.alignItems = Align.Center; // Center image
            itemSlot.style.width = 120;
            itemSlot.style.height = 120;
            itemSlot.style.marginRight = 10; // Spacing between columns
            itemSlot.style.marginBottom = 10; // Spacing between rows
            itemSlot.style.backgroundColor = SlotColor;

            VisualElement itemImage = new VisualElement();
            itemImage.style.width = 120;
            itemImage.style.height = 120;
            itemImage.style.alignSelf = Align.Center;

            if (i < items.Count)
            {
                Item item = items[index];
                itemImage.style.backgroundImage = new StyleBackground(item.Sprite);

                itemSlot.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    itemSlot.style.backgroundColor = HoverColor;
                    tooltip.Show(item, goldText(item));
                    moveTooltip(evt.mousePosition);
                });
                itemSlot.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    itemSlot.style.backgroundColor = isSelected != null && isSelected(index) ? SelectedColor : SlotColor;
                    tooltip.Hide();
                });
                itemSlot.RegisterCallback<MouseMoveEvent>(evt => moveTooltip(evt.mousePosition));
                itemSlot.RegisterCallback<ClickEvent>(evt =>
                {
                    tooltip.Hide();
                    onClick(itemSlot, index);
                });
            }

            itemSlot.Add(itemImage);
            gridContainer.Add(itemSlot);
        }
        return gridContainer;
    }

    // Wraps a grid in its own vertical ScrollView with slower mouse-wheel scrolling
    public static ScrollView WrapInScrollView(VisualElement grid)
    {
        ScrollView gridScrollView = new ScrollView(ScrollViewMode.Vertical);
        gridScrollView.style.width = Length.Percent(100);
        gridScrollView.style.overflow = Overflow.Hidden; // Prevents content overflow
        gridScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        gridScrollView.RegisterCallback<WheelEvent>(evt =>
        {
            gridScrollView.scrollOffset += new Vector2(0, evt.delta.y * 0.25f); // Adjust speed if needed
            evt.StopPropagation();
        });
        gridScrollView.Add(grid);
        return gridScrollView;
    }
}
