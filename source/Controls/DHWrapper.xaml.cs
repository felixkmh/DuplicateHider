using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DuplicateHider.Controls
{
    /// <summary>
    /// Interaktionslogik für DHWrapper.xaml
    /// </summary>
    public partial class DHWrapper : Playnite.SDK.Controls.PluginUserControl
    {
        public DHWrapper()
        {
            InitializeComponent();
        }

        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            if (DH_ContentControl.Style == null)
            {
                if (Parent is ContentControl parent)
                {
                    if (parent.Tag is Style style)
                    {
                        DH_ContentControl.Style = style;
                    }
                }
            }
            DH_ContentControl.GameContext = newContext;
            DH_ContentControl.GameContextChanged(oldContext, newContext);
        }

    }
}
