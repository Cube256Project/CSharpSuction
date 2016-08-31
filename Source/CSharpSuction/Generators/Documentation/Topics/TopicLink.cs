using System;

namespace CSharpSuction.Generators.Documentation.Topics
{
    /// <summary>
    /// Describes a link between two topics.
    /// </summary>
    class TopicLink
    {
        /// <summary>
        /// The topic where the link originates, the left side.
        /// </summary>
        public string ReferingTopic;

        /// <summary>
        /// The kind of relation.
        /// </summary>
        public TopicRelation Kind;

        /// <summary>
        /// The target of the link, the right side.
        /// </summary>
        public string ReferencedTopic;

        public TopicLink(string from, TopicRelation how, string to)
        {
            ReferingTopic = from;
            Kind = how;
            ReferencedTopic = to;
        }

        public override bool Equals(object obj)
        {
            if (obj is TopicLink)
            {
                var other = (TopicLink)obj;
                return
                    ReferencedTopic.Equals(other.ReferencedTopic) &&
                    Kind.GetType() == other.Kind.GetType() &&
                    ReferingTopic.Equals(other.ReferingTopic);

            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return ReferencedTopic.GetHashCode() + Kind.GetType().GetHashCode() + ReferingTopic.GetHashCode();
        }
    }
}
