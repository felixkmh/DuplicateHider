using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateHider.Models
{
    public class EnabledFieldsModel : ObservableObject
    {
        protected bool name;
        public bool Name { get => name; set => SetValue(ref name, value); }

        protected bool sortingName;
        public bool SortingName { get => sortingName; set => SetValue(ref sortingName, value); }

        protected bool platforms;
        public bool Platforms { get => platforms; set => SetValue(ref platforms, value); }

        protected bool platformsUnion;
        public bool PlatformsUnion { get => platformsUnion; set => SetValue(ref platformsUnion, value); }

        protected bool genres;
        public bool Genres { get => genres; set => SetValue(ref genres, value); }

        protected bool genresUnion;
        public bool GenresUnion { get => genresUnion; set => SetValue(ref genresUnion, value); }

        protected bool developers;
        public bool Developers { get => developers; set => SetValue(ref developers, value); }

        protected bool developersUnion;
        public bool DevelopersUnion { get => developersUnion; set => SetValue(ref developersUnion, value); }

        protected bool publishers;
        public bool Publishers { get => publishers; set => SetValue(ref publishers, value); }

        protected bool publishersUnion;
        public bool PublishersUnion { get => publishersUnion; set => SetValue(ref publishersUnion, value); }

        protected bool categories;
        public bool Categories { get => categories; set => SetValue(ref categories, value); }

        protected bool categoriesUnion;
        public bool CategoriesUnion { get => categoriesUnion; set => SetValue(ref categoriesUnion, value); }

        protected bool features;
        public bool Features { get => features; set => SetValue(ref features, value); }

        protected bool featuresUnion;
        public bool FeaturesUnion { get => featuresUnion; set => SetValue(ref featuresUnion, value); }

        protected bool completionStatus;
        public bool CompletionStatus { get => completionStatus; set => SetValue(ref completionStatus, value); }

        protected bool tags;
        public bool Tags { get => tags; set => SetValue(ref tags, value); }

        protected bool tagsUnion;
        public bool TagsUnion { get => tagsUnion; set => SetValue(ref tagsUnion, value); }

        protected bool description;
        public bool Description { get => description; set => SetValue(ref description, value); }

        protected bool releaseDate;
        public bool ReleaseDate { get => releaseDate; set => SetValue(ref releaseDate, value); }

        protected bool series;
        public bool Series { get => series; set => SetValue(ref series, value); }

        protected bool seriesUnion;
        public bool SeriesUnion { get => seriesUnion; set => SetValue(ref seriesUnion, value); }

        protected bool ageRestriction;
        public bool AgeRatings { get => ageRestriction; set => SetValue(ref ageRestriction, value); }

        protected bool ageRestrictionUnion;
        public bool AgeRatingsUnion { get => ageRestrictionUnion; set => SetValue(ref ageRestrictionUnion, value); }

        protected bool regions;
        public bool Regions { get => regions; set => SetValue(ref regions, value); }

        protected bool regionsUnion;
        public bool RegionsUnion { get => regionsUnion; set => SetValue(ref regionsUnion, value); }

        protected bool version;
        public bool Version { get => version; set => SetValue(ref version, value); }

        protected bool userScore;
        public bool UserScore { get => userScore; set => SetValue(ref userScore, value); }

        protected bool criticsScore;
        public bool CriticsScore { get => criticsScore; set => SetValue(ref criticsScore, value); }

        protected bool communityScore;
        public bool CommunityScore { get => communityScore; set => SetValue(ref communityScore, value); }

        protected bool backgroungImage;
        public bool BackgroundImage { get => backgroungImage; set => SetValue(ref backgroungImage, value); }

        protected bool iconImage;
        public bool Icon { get => iconImage; set => SetValue(ref iconImage, value); }

        protected bool coverImage;
        public bool CoverImage { get => coverImage; set => SetValue(ref coverImage, value); }

        protected bool links;
        public bool Links { get => links; set => SetValue(ref links, value); }

        protected bool linksUnion;
        public bool LinksUnion { get => linksUnion; set => SetValue(ref linksUnion, value); }

        protected bool logo;
        public bool Logo { get => logo; set => SetValue(ref logo, value); }

        protected bool trailer;
        public bool Trailer { get => trailer; set => SetValue(ref trailer, value); }

        protected bool microTrailer;
        public bool MicroTrailer { get => microTrailer; set => SetValue(ref microTrailer, value); }
    }
}
