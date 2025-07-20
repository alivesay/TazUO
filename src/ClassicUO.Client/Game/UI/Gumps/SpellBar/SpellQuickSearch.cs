using System;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.SpellBar;

public class SpellQuickSearch : Gump
{
    private TTFTextInputField searchField;
    private SpellDisplay spellDisplay;
    private Action<SpellDefinition>  onClick;
    private bool disposeOnReturn;
    
    public SpellQuickSearch(int x, int y, Action<SpellDefinition> onClick = null, bool disposeOnReturn = false) : base(0, 0)
    {
        Width = 200;
        Height = 69;
        CanMove = true;
        X = x;
        Y = y;
        this.disposeOnReturn = disposeOnReturn;
        this.onClick = onClick;

        Build();
    }

    public override bool AcceptMouseInput { get; set; } = true;

    private void Build()
    {
        Add(new AlphaBlendControl(0.75f){Width = Width, Height = Height});
        
        Add(searchField = new TTFTextInputField(Width, 25, Width){Y = Height - 25});
        searchField.SetPlaceholder("Search..");
        searchField.SetFocus();
        
        searchField.TextChanged += SearchTextChanged;
        searchField.EnterPressed += SearchEnterPressed;
        
        Add(spellDisplay = new(Width, onClick, disposeOnReturn){Y = Height - 25 - 44});
    }

    private void SearchEnterPressed(object sender, EventArgs e)
    {
        if (IsDisposed)
            return;

        searchField.OnFocusLost();
        UIManager.KeyboardFocusControl = null;
        spellDisplay.InvokeAction();
        if(disposeOnReturn)
            Dispose();
    }

    private void SearchTextChanged(object sender, EventArgs e)
    {
        if (IsDisposed)
            return;
        
        if(searchField?.Text.Length > 0)
        {
            if (SpellDefinition.TryGetSpellFromName(searchField?.Text, out var spell))
                spellDisplay.SetSpell(spell);
        }
        else
        {
            spellDisplay.ClearSpell();
        }
    }

    private class SpellDisplay : Control
    {
        private GumpPic icon;
        private TextBox text;
        private Action<SpellDefinition> onClick;
        private bool disposeOnReturn;
        
        public SpellDefinition Spell;
        public SpellDisplay(int width, Action<SpellDefinition> onClick, bool disposeOnReturn)
        {
            Width = width;
            Height = 44;
            CanMove = true;
            this.onClick = onClick;
            this.disposeOnReturn = disposeOnReturn;
        }
        
        public override bool AcceptMouseInput { get; set; } = true;

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            
            InvokeAction();
            
            if(disposeOnReturn)
                Parent?.Dispose();
        }

        public void InvokeAction()
        {
            onClick?.Invoke(Spell);
        }

        public void ClearSpell()
        {
            Spell = null;
            icon?.Dispose();
            text?.Dispose();
        }

        public void SetSpell(SpellDefinition spell)
        {
            Spell = spell;
            icon?.Dispose();
            //Intential to only use height, these are square spell icons.
            Add(icon = new GumpPic(0, 0, (ushort)spell.GumpIconSmallID, 0) { AcceptMouseInput = false, Width = Height, Height = Height });
            Color color = Color.White;

            switch (spell.TargetType)
            {
                case TargetType.Harmful: color = Color.OrangeRed; break;

                case TargetType.Beneficial: color = Color.GreenYellow; break;
            }
            
            text?.Dispose();
            Add(text = TextBox.GetOne(spell.Name, TrueTypeLoader.EMBEDDED_FONT, 18, color, TextBox.RTLOptions.Default(Width - Height)));
            text.X = Height;
            text.Y = (Height - text.Height) >> 1;
        }
    }
}