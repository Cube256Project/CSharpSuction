namespace CSharpSuction.Documentation
{
    /// <summary>
    /// Describes a link between two topics.
    /// </summary>
    class TopicLink
    {
        /// <summary>
        /// The topic where the link originates.
        /// </summary>
        public string ReferingTopic;

        /// <summary>
        /// The kind of relation.
        /// </summary>
        public TopicRelation Kind;

        /// <summary>
        /// The target of the link.
        /// </summary>
        public string ReferencedTopic;

        public TopicLink(string from, TopicRelation how, string to)
        {
            ReferingTopic = from;
            Kind = how;
            ReferencedTopic = to;
        }
    }
}
