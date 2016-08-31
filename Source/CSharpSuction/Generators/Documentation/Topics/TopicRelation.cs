namespace CSharpSuction.Generators.Documentation.Topics
{
    /// <summary>
    /// Describes the kind of relation two topics have.
    /// </summary>
    /// <remarks>
    /// <para>The relation has a left and a right side. The left side is assumed to be 
    /// the topic making the reference, while the right side is the referenced topic.</para>
    /// </remarks>
    abstract class TopicRelation
    {
        /// <summary>
        /// Relation name derived from type name.
        /// </summary>
        public string Name { get { return GetType().Name; } }
    }

    /// <summary>
    /// States that left is a base class of right.
    /// </summary>
    class TopicIsBaseClassOf : TopicRelation { }

    /// <summary>
    /// States that left is derived from right.
    /// </summary>
    class TopicIsDerivedFrom : TopicRelation { }

    class TopicReferences : TopicRelation { }

    /// <summary>
    /// States that left is a namespace member of right.
    /// </summary>
    class TopicContainingNamespace : TopicRelation { }

    /// <summary>
    /// States that left contains right as a namespace member.
    /// </summary>
    class TopicNamespaceContains : TopicRelation { }
}
