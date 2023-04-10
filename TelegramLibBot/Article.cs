using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLibBot
{
    internal class Article
    {
        private string title;
        private string genre;
        private string author;
        private string description;

        public string Title { get; init; }
        public string Genre { get; init; }
        public string Author { get; init; }
        public string Description { get; init; }


        public Article(string title, string genre, string author, string description)
        {
            if(string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(genre)
                || string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(description.ToString()))
            {
                throw new ArgumentNullException("Один из аргументов не имеет значения");
            }

            Title = title;
            Genre = genre;
            Author = author;
            Description = description;
        }

      

        public override string ToString()
        {
            return $"{Title}\nЖанры:{Genre}\n\n{Description.ToString()}\n\nАвтор:{Author}";
        }

    }
}
