namespace CSharpSuction.Documentation
{
    /// <summary>
    /// Describes the kind of relation two topics have.
    /// </summary>
    abstract class TopicRelation
    {
        public string Name { get { return GetType().Name; } }
    }

    class TopicIsBaseClassOf : TopicRelation { }

    class TopicIsDerivedFrom : TopicRelation { }

    class TopicReferences : TopicRelation { }

    class TopicContainingNamespace : TopicRelation { }

    class TopicNamespaceContains : TopicRelation { }
}
