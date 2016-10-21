﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Collections.ObjectModel;

using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.QueryStringDotNET;
using System.Linq;
using System.Diagnostics;

namespace Doroish {

    public sealed partial class MainPage : Page {
        public ObservableCollection<Doro> DoroList;
        private DoroTimer DoroTimer;
        private DispatcherTimer UITimer = new DispatcherTimer();
        private Random Rand = new Random();

        public  MainPage() {
            InitializeComponent();
            DoroList = new ObservableCollection<Doro>();
        }


        private async void AddButton_Click(object sender, RoutedEventArgs e) {

            var options = new Windows.System.LauncherOptions();


            var dialog = new DoroDialog();
            var result = await dialog.ShowAsync();

            if(result == ContentDialogResult.Primary) {
                var doro = dialog.Doro;
                DoroList.Add(doro);
            }

            if(DoroList.Count > 0) {
                EmptyStateTextBlock.Visibility = Visibility.Collapsed;
            }

        }


        private void RemoveButton_Click(object sender, RoutedEventArgs e) {
            if(DoroListView.SelectedIndex == -1)
                return;
            DoroList.RemoveAt(DoroListView.SelectedIndex);

            if(DoroList.Count == 0) {
                EmptyStateTextBlock.Visibility = Visibility.Visible;
            }
        }


        private void DoroTimer_Tick(DoroTimerEvent e) {
            if(e.EventDescription == DoroTimerEvent.FINISHED_DORO) {
                ShowNotification(e.Doro);
                Debug.WriteLine("Finished Doro");
            }

            if(e.EventDescription == DoroTimerEvent.FINISHED) {
                UITimer.Stop();

                StatusBar.Text = "Finished";
                
                AddButton.IsEnabled = true;
                RemoveButton.IsEnabled = true;
                StartButton.Icon = new SymbolIcon(Symbol.Play);
            }
        }


        private void UITimer_Tick(object sender, object e) {
            var CurrentDoro = DoroTimer.CurrentDoro;

            if (DoroTimer.IsBreak) {
                var timeElapsed = DoroTimer.BreakElapsed;

                StatusBar.Text = string.Format("{0}: {1:00}:{2:00} / {3:00}:{4:00}", "Break", timeElapsed.Minutes, timeElapsed.Seconds, CurrentDoro.BreakDuration.Minutes, CurrentDoro.BreakDuration.Seconds);
            } else {
                var timeElapsed = DoroTimer.Elapsed;

                StatusBar.Text = string.Format("{0}: {1:00}:{2:00} / {3:00}:{4:00}", CurrentDoro.Title, timeElapsed.Minutes, timeElapsed.Seconds, CurrentDoro.Duration.Minutes, CurrentDoro.Duration.Seconds);
            }
        }


        private void StartButton_Click(object sender, RoutedEventArgs e) {

            if(DoroList.Count == 0) {
                return;
            }

            if(DoroTimer == null || !DoroTimer.IsRunning) {
                DoroTimer = new DoroTimer(DoroList.ToList());

                DoroTimer.Tick += DoroTimer_Tick;

                DoroTimer.Start();
                UITimer.Start();

                UITimer.Tick += UITimer_Tick;
                UITimer.Interval = new TimeSpan(0, 0, 1);
                UITimer.Start();

                AddButton.IsEnabled = false;
                RemoveButton.IsEnabled = false;
                StartButton.Icon = new SymbolIcon(Symbol.Stop);

                UITimer_Tick(null, null);
            } else {
                DoroTimer.Stop();
                UITimer.Stop();

                StatusBar.Text = "Stopped";

                AddButton.IsEnabled = true;
                RemoveButton.IsEnabled = true;
                StartButton.Icon = new SymbolIcon(Symbol.Play);
            }

        }

        private void ShowNotification(Doro doro) {
            string title = doro.Title + " has finished";

            // construct the visuals of the toast
            ToastVisual visual = new ToastVisual() {
                BindingGeneric = new ToastBindingGeneric() {
                    Children = {
                        new AdaptiveText() { Text = title }
                    }
                }
            };

            int conversationId = Rand.Next();

            // construct the actions for the toast (inputs and buttons)
            ToastActionsCustom actions = new ToastActionsCustom() {
                Inputs = {
                    new ToastTextBox("tbNote") { PlaceholderContent = "Add a note" }
                },
                Buttons = {
                    new ToastButton("Add Note", new QueryString() {
                        { "action", "reply" },
                        { "conversationId", conversationId.ToString() },
                        { "dorotitle", doro.Title }
                    }.ToString()) {
                        ActivationType = ToastActivationType.Foreground,
                        TextBoxId = "tbNote"
                    }
                }
            };

            // construct the final toast content
            ToastContent toastContent = new ToastContent() {
                Visual = visual,
                Actions = actions,
                Launch = new QueryString() {
                    { "action", "viewConversation" },
                    { "conversationId", conversationId.ToString() }
                }.ToString()
            };

            // And create the toast notification
            var toast = new ToastNotification(toastContent.GetXml());

            toast.ExpirationTime = DateTime.Now.AddDays(2);

            toast.Tag = "1";
            toast.Group = "doro" + doro.Title;
            toast.SuppressPopup = false;

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
