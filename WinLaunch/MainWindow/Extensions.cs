using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WinLaunch
{
    partial class MainWindow : Window
    {
        private void ExtensionsToggle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ToggleToolbar();
        }

        private bool EditExtensionActive = false;

        private void RunEditExtension(SBItem Item)
        {
            //disable hotkey
            ActivatorsEnabled = false;

            //activate extension
            EditItem EditItemDialog = new EditItem(this, Item);
            EditItemDialog.Owner = this;
            if ((bool)EditItemDialog.ShowDialog())
            {
                //rerender item
                if (Item.IsFolder)
                {
                    if (SBM.FolderOpen && SBM.ActiveFolder == Item)
                    {
                        //Update FolderTitle
                        if (Theme.CurrentTheme.UseVectorFolder)
                        {
                            FolderTitle.Text = Item.Name;
                        }
                        else
                        {
                            FolderTitleNew.Text = Item.Name;
                        }
                    }
                }

                TriggerSaveItemsDelayed();
                TriggerUpdateAssistantItemsTimer();
            }

            //end extension
            SBM.ExtensionActive = false;
            SBM.LockItems = false;

            ActivatorsEnabled = true;
        }

        private bool extensionBarVisible = false;

        private void HideToolbar()
        {
            extensionBarVisible = false;

            DoubleAnimation anim = new DoubleAnimation()
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(200),
                DecelerationRatio = .4
            };

            ExtensionsGrid.BeginAnimation(Grid.HeightProperty, anim);

            //animate extensionstoggle
            DoubleAnimation toggleanim = new DoubleAnimation()
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(200),
                DecelerationRatio = .4
            };

            ExtensionsToggleRotation.BeginAnimation(RotateTransform.AngleProperty, toggleanim);
        }

        private void ToggleToolbar()
        {
            //Toggle extensionsbar
            if (extensionBarVisible)
            {
                extensionBarVisible = false;

                DoubleAnimation anim = new DoubleAnimation()
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    DecelerationRatio = .4
                };

                ExtensionsGrid.BeginAnimation(Grid.HeightProperty, anim);

                //animate extensionstoggle
                DoubleAnimation toggleanim = new DoubleAnimation()
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    DecelerationRatio = .4
                };

                ExtensionsToggleRotation.BeginAnimation(RotateTransform.AngleProperty, toggleanim);

                if (!AssistantActive)
                {
                    //make page counters visible again 
                    PageCounterWrap.Visibility = Visibility.Visible;
                }
            }
            else
            {
                //make visible
                extensionBarVisible = true;
                DoubleAnimation anim = new DoubleAnimation()
                {
                    To = 130.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    DecelerationRatio = .4
                };

                ExtensionsGrid.BeginAnimation(Grid.HeightProperty, anim);

                //animate extensionstoggle
                DoubleAnimation toggleanim = new DoubleAnimation()
                {
                    To = 45.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    DecelerationRatio = .4
                };

                ExtensionsToggleRotation.BeginAnimation(RotateTransform.AngleProperty, toggleanim);


                //hide page dots 
                PageCounterWrap.Visibility = Visibility.Collapsed;
            }
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            Grid grid = sender as Grid;

            DoubleAnimation anim = new DoubleAnimation()
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(100),
                DecelerationRatio = .4
            };

            grid.BeginAnimation(Grid.OpacityProperty, anim);

            e.Handled = true;
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            Grid grid = sender as Grid;

            DoubleAnimation anim = new DoubleAnimation()
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(200),
                DecelerationRatio = .4
            };

            grid.BeginAnimation(Grid.OpacityProperty, anim);

            e.Handled = true;
        }
    }
}