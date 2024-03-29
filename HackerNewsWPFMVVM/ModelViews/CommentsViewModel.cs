﻿using HackerNewsWPFMVVM.Helpers;
using HackerNewsWPFMVVM.Models.Api;
using HackerNewsWPFMVVM.Models.Data;
using HackerNewsWPFMVVM.ModelViews.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace HackerNewsWPFMVVM.ModelViews
{
    public class CommentsViewModel : ObservableCollection<CommentModel>
    {
        HackerNewsEndPoint EndPoint = Singleton.EndPoint;
        public GetCommentsCommand GetCommentsCommand { get; set; }
        public MainWindow MV;

        public List<string> MenuItems { get; set; }
        public int ID { get; set; }
        private int CurrentParentId;

        public CommentsViewModel()
        {
            MV = ((MainWindow)Application.Current.MainWindow);

            MenuItems = new List<string>();
            MenuItems.Add("Back");

            this.GetCommentsCommand = new GetCommentsCommand(this);

            GetComments(((MainWindow)Application.Current.MainWindow).StoryId);

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Add(new CommentModel { By = "me", Time = 1000, Text = "Pollo" });
                Add(new CommentModel { By = "me", Time = 1000, Text = "Empanadas" });
                Add(new CommentModel { By = "me", Time = 1000, Text = "Chipas" });
            }
        }

        public async void GetComments(int parentId)
        {
            var result = await EndPoint.GetComments(parentId, "story");

            foreach (var item in result)
            {
                if (item.Text != null)
                {
                    item.TimeSpan = TimeConverter.TimeSpanToString(item.Time);
                    item.Text = HtmlParser.Parse(item.Text);
                    Add(item);
                }
            }
        }

        public async void GetMoreComments(int parentId)
        {
            CurrentParentId = parentId;
            var result = await EndPoint.GetComments(parentId, "comment");

            //var parent = this.Where(z => z.Id == parentId).FirstOrDefault();

            GetCommentsResponse commentResponse = null;
            foreach (var item in this)
            {
                commentResponse = GetCommentTree(item, parentId, item.Id);
                if (commentResponse != null)
                {
                    break;
                }
            }

            CommentModel parent = commentResponse.Comment;
            int TopCommentId = commentResponse.TopCommentId;
            parent.Children = new List<CommentModel>();

            foreach (var item in result)
            {
                if (item.Text != null)
                {
                    item.TimeSpan = TimeConverter.TimeSpanToString(item.Time);
                    item.Text = HtmlParser.Parse(item.Text);
                    parent.Children.Add(item);
                }
            }

            var itemToUpdate = this.Single(r => r.Id == TopCommentId);
            var index = this.IndexOf(itemToUpdate);
            this.InsertItem(index, itemToUpdate);
            this.RemoveAt(index + 1);
            //this.Remove(itemToRemove);
            CurrentParentId = 0;
        }

        public GetCommentsResponse GetCommentTree(CommentModel item, int currentNodeId, int topCommentId)
        {
            if (item.Id != currentNodeId)
            {
                if (item.Children != null)
                {
                    GetCommentsResponse foundresult = null;
                    foreach (var inner in item.Children)
                    {
                        foundresult = GetCommentTree(inner, currentNodeId, topCommentId);
                        if (foundresult != null)
                        {
                            break;
                        }
                    }

                    return foundresult;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return new GetCommentsResponse
                {
                    Comment = item,
                    TopCommentId = topCommentId
                };
            }
        }

        public bool CheckIsLoading(int currentId)
        {
            if (currentId == CurrentParentId)
                return false;
            return true;
        }

        public void ChangeContext()
        {
            Application.Current.Windows[0].DataContext = MV.StoriesModel;
        }

        public void RaisePropertyChanged(string s)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(s));
        }
    }
}