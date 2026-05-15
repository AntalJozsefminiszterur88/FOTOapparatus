using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FOTOapparatus.UI;

public sealed record DialogButtonDefinition(
    string Label,
    string Result,
    bool IsDefault = false,
    bool IsDestructive = false);

internal partial class MessageDialogWindow : Window
{
    public MessageDialogWindow(string title, string message, params DialogButtonDefinition[] buttons)
    {
        InitializeComponent();

        Title = title;
        MessageTextBlock.Text = message;

        foreach (var definition in buttons)
        {
            var button = new Button
            {
                Content = definition.Label,
                MinWidth = 92,
            };

            if (definition.IsDefault)
            {
                button.Classes.Add("primary");
            }

            if (definition.IsDestructive)
            {
                button.Classes.Add("danger");
            }

            button.Click += (_, _) => Close(definition.Result);
            ButtonPanel.Children.Add(button);
        }
    }


}
