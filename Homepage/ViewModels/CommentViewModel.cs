using Homepage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Homepage.ViewModels
{
    public class CommentViewModel
    {
        public CommentId Id { get; }
        public string UserName { get; }
        public string DateText { get; }
        public string Text { get; }

        public CommentViewModel(CommentId id, string userName, DateTime dateTime, string text)
        {
            var delta = DateTime.Now - dateTime;
            if (delta.TotalMinutes <= 2)
            {
                DateText = "방금";
            }
            else if (delta.TotalHours <= 1)
            {
                DateText = $"{Math.Floor(delta.TotalMinutes)}분 전";
            }
            else if (delta.TotalHours < 24)
            {
                DateText = $"{Math.Floor(delta.TotalHours)}시간 전";
            }
            else
            {
                DateText = dateTime.ToShortDateString();
            }

            Id = id;
            UserName = userName;            
            Text = text;
        }
    }
}
