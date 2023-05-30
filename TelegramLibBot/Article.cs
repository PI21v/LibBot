using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLibBot
{
    internal class Article
    {
        private string postType;
        private string tegs;
        private string title;
        private string genre;
        private string author;
        private string description;

        public string PostType { get; init; }
        public string Title { get; init; }
        public string Genre { get; init; }
        public string Tegs { get; set; }
        public string Author { get; init; }
        public string Description { get; init; }
        
        public Article(string title, string genre, string author, string description,string postType,string tegs)
        {
            if(string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(genre)
                || string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(description.ToString())
                || string.IsNullOrWhiteSpace(postType) || string.IsNullOrWhiteSpace(tegs))
            {
                throw new ArgumentNullException("Один из аргументов не имеет значения");
            }
            PostType = postType;
            Title = title;
            Genre = genre;
            Author = author;
            Description = description;
            Tegs = tegs;
        }

      
        public override string ToString()
        {
            return $"{Title}\nТип поста:{PostType}\nЖанры:{Genre}\nТеги:{Tegs}\n\n{Description.ToString()}\n\nАвтор:{Author}";
        }
    }
}
