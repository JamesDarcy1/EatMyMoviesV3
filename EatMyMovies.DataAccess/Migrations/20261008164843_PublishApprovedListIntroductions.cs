using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatMyMovies.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PublishApprovedListIntroductions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (name, before, after) in Introductions)
                UpdateDescription(migrationBuilder, name, before, after);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (name, before, after) in Introductions)
                UpdateDescription(migrationBuilder, name, after, before);
        }

        // Only replace the published copy reviewed by James. A later admin edit is never overwritten.
        private static void UpdateDescription(MigrationBuilder migrationBuilder, string name, string before, string after)
        {
            static string Literal(string value) => "N'" + value.Replace("'", "''") + "'";
            migrationBuilder.Sql($"UPDATE [Lists] SET [Description] = {Literal(after)} WHERE [Name] = {Literal(name)} AND [Description] = {Literal(before)};");
        }

        private static readonly (string Name, string Before, string After)[] Introductions =
        [
            (
                "Top 100",
                "Get set for the ultimate movie binge as we unveil the top 100 films of all time. From classics to modern hits, these cinematic wonders have shaped our movie nights and stirred our hearts. Grab your snacks and lets dive into the magic of storytelling!",
                "These are the films I would most readily return to or recommend to someone who asks where to start. I have weighed the lasting impression each one made on me as much as its reputation, so this is a personal ranking rather than a tally of awards or review scores. Use it as a route into old favourites and a few discoveries you might have missed."
            ),
            (
                "Comedies",
                "Get ready to laugh till your sides hurt as we roll out the red carpet for the funniest flicks of all time. From timeless classics to modern chuckle-fests, these comedy gems are guaranteed to lift your spirits and leave you in stitches. So grab your popcorn, kick back, and get ready for a comedy extravaganza that'll have you LOL-ing from start to finish!",
                "A comedy earns a place here when its humour still works after the surprise has gone. I have looked for films with memorable characters, sharp timing and the sort of scenes you want to share with someone else. Some are broad and silly; others find their laughs in awkwardness or warmth."
            ),
            (
                "Foreign Films",
                "Take a trip around the globe without leaving your seat as we celebrate the finest offerings from international cinema. From captivating dramas to breathtaking adventures, these foreign-language films offer a window into different cultures and perspectives, enriching our cinematic experience in ways we never imagined. So broaden your horizons, open your heart, and get ready to be transported to faraway lands through the magic of storytelling!",
                "The language of a film should never be a barrier to a great movie night. These picks travel across countries, styles and eras, but each offers a perspective I would have missed by staying with English-language cinema alone. I ranked them by how strongly their stories and images stayed with me, not by how familiar their titles are."
            ),
            (
                "Documentaries",
                "Embark on a journey of discovery with our curated list of must-watch documentaries. From gripping true stories to eye-opening explorations of the world around us, these films will inspire, educate, and captivate.",
                "The best documentaries leave me seeing a familiar subject differently or curious enough to keep reading after the credits. This list favours clear storytelling and a distinctive point of view over a simple catalogue of facts. The topics vary, but every selection gave me a reason to talk about it afterwards."
            ),
            (
                "Christmas",
                "'Tis the season to be jolly as we unwrap a sleighful of festive favorites that capture the magic and joy of the holidays. From heartwarming classics to merry modern marvels, these Christmas films are the perfect way to spread holiday cheer and create lasting memories with family and friends. So pour yourself a cup of hot cocoa, snuggle up by the fireplace, and get ready to celebrate the most wonderful time of the year with these merry movies!",
                "This list is for the film you put on while the decorations are up, whether you want comfort, chaos or a little of both. I have mixed seasonal staples with picks that earn a repeat viewing through their characters and atmosphere. The ranking reflects how readily I would make room for each one on a December movie night."
            ),
            (
                "Standout Soundtracks",
                "This list celebrates films where the soundtrack truly stands out. From different decades and genres, each movie here features a soundtrack so compelling, you'll find yourself listening to it on repeat long after the credits roll.",
                "Some films stay with you because you can hear them long after you have forgotten a scene. These selections use songs or score to shape the mood, sharpen a moment or carry you back into their world. I ranked them by how much the music adds to the experience of watching the film, not simply by how many familiar tracks appear."
            ),
            (
                "Iconic 80s",
                "Step back into the vibrant decade of neon lights, big hair, and unforgettable movies. This list is all about the '80s—an era that gave us some of the most iconic films in cinema history. From classic blockbusters to cult favorites, these movies defined a generation and continue to influence pop culture today. Whether you're reliving the nostalgia or discovering these gems for the first time, these films are a must-watch.",
                "The 1980s gave us films with unmistakable energy, looks and lines that still turn up in conversation. This list reaches across genres to find the ones I think remain enjoyable as films, beyond the nostalgia attached to them. My order favours the movies I would happily put on again today."
            ),
            (
                "Disney",
                "Discover the magic of Disney with this handpicked list of must-watch films, featuring timeless classics and modern hits perfect for all ages",
                "These are Disney films I think repay another watch, whether it is for the characters, the craft or the songs. The list mixes different periods and moods rather than treating box-office success as the deciding factor. I ranked them by the connection they made with me and how well that feeling has lasted."
            ),
            (
                "Horrors",
                "Dare to watch the scariest horror films ever made, from relentless psychological terrors to spine-chilling supernatural horrors that will haunt your nightmares!",
                "Horror can unsettle through a sudden scare, a creeping idea or a mood that refuses to lift. I have included films that use those tools with purpose and leave something behind after the immediate shock. The order reflects which experiences stayed with me most, rather than a count of jump scares."
            )
        ];
    }
}
