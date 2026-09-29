using UnityEngine;
using UnityEngine.UIElements;

// Hover tooltip (name, gold line, description) shared by the inventory, equipment, shop and loot windows.
// Each window positions it itself because their layouts need different offsets.
public class ItemTooltip
{
    private readonly Label root;
    private readonly Label nameLabel;
    private readonly Label goldLabel;
    private readonly Label descriptionLabel;

    public float Width => root.resolvedStyle.width;
    public float Height => root.resolvedStyle.height;

    public ItemTooltip(VisualElement container)
    {
        root = new Label();
        root.style.position = Position.Absolute;
        root.style.backgroundColor = new Color(0, 0, 0, 0.8f);
        root.style.color = Color.white;
        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        root.style.paddingTop = 5;
        root.style.paddingBottom = 5;
        root.style.fontSize = 24;
        root.style.maxWidth = 500;
        root.style.visibility = Visibility.Hidden;

        nameLabel = CreateLabel(28, Color.white, 5);
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        goldLabel = CreateLabel(22, Color.yellow, 5);
        descriptionLabel = CreateLabel(22, Color.white, 0);

        root.Add(nameLabel);
        root.Add(goldLabel);
        root.Add(descriptionLabel);
        container.Add(root);
    }

    private static Label CreateLabel(int fontSize, Color color, float marginBottom)
    {
        Label label = new Label();
        label.style.fontSize = fontSize;
        label.style.color = color;
        label.style.marginBottom = marginBottom; // Space before the next line
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.overflow = Overflow.Hidden;
        label.style.textOverflow = TextOverflow.Clip;
        return label;
    }

    public void Show(Item item, string goldText)
    {
        item.AdjustDescription(); // Ensure description updates dynamically
        nameLabel.text = item.Name;
        goldLabel.text = goldText;
        descriptionLabel.text = item.Description;
        root.style.visibility = Visibility.Visible;
    }

    public void Hide()
    {
        root.style.visibility = Visibility.Hidden;
    }

    public void MoveTo(float left, float top)
    {
        root.style.left = left;
        root.style.top = top;
    }
}
