using MvvmCross.Platforms.Wpf.Presenters;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TerminalPOS.WPF.ViewPresenters
{
    public class TerminalPosMvxContentPresenter : MvxWpfViewPresenter
    {
        private string navFrameName;

        public TerminalPosMvxContentPresenter(ContentControl contentControl, string navFrameName)
            : base(contentControl)
        {
            this.navFrameName = navFrameName;
        }

        protected override Task<bool> ShowContentView(FrameworkElement element, MvxContentPresentationAttribute attribute, MvxViewModelRequest request)
        {
            var contentControl = FrameworkElementsDictionary.Keys.FirstOrDefault(w => (w as MvxWindow)?.Identifier == attribute.WindowIdentifier) ?? FrameworkElementsDictionary.Keys.Last();

            if (!attribute.StackNavigation && FrameworkElementsDictionary[contentControl].Any())
                FrameworkElementsDictionary[contentControl].Pop(); // Close previous view



            var containerView = FindChild<Frame>(contentControl, navFrameName) as ContentControl;
            if (containerView != null)
            {
                if (FrameworkElementsDictionary.ContainsKey(containerView) == false)
                {
                    FrameworkElementsDictionary.Add(containerView, new Stack<FrameworkElement>());
                }
                FrameworkElementsDictionary[containerView].Push(element);
                containerView.Content = element;
                return Task.FromResult(true);
            }

            return base.ShowContentView(element, attribute, request);
        }

        // Implementation from: http://stackoverflow.com/a/1759923/80186
        internal static T FindChild<T>(DependencyObject reference, string childName) where T : DependencyObject
        {
            // Confirm parent and childName are valid.
            if (reference == null) return null;

            var foundChild = default(T);
            var nextPhase = new List<DependencyObject>();

            var childrenCount = VisualTreeHelper.GetChildrenCount(reference);
            for (var index = 0; index < childrenCount; index++)
            {
                var child = VisualTreeHelper.GetChild(reference, index);

                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                    else
                    {
                        // keep for searching inside this frame
                        nextPhase.Add(child);
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            // if failed to find the child, search inside the frames we found
            if (foundChild == null)
            {
                foreach (var item in nextPhase)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(item, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;

                }
            }

            return foundChild;
        }
    }
}
