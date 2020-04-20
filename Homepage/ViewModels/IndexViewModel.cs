using Homepage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Homepage.ViewModels
{
    public class IndexViewModel
    {
        public List<CommentViewModel> CommentVMs { get; }
        
        public IndexViewModel(List<CommentViewModel> commentVMs)
        {
            CommentVMs = commentVMs;
        }
    }
}
