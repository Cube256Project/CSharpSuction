using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace CSharpSuction
{
    class TreeCollection : IEnumerable<SyntaxTree>
    {
        private HashSet<SyntaxTree> _trees = new HashSet<SyntaxTree>();

        public void Add(SyntaxTree tree)
        {
            if(!_trees.Contains(tree))
            {
                _trees.Add(tree);
            }
        }

        public void AddRange(IEnumerable<SyntaxTree> trees)
        {
            foreach(var tree in trees)
            {
                _trees.Add(tree);
            }
        }

        public bool Contains(SyntaxTree tree)
        {
            return _trees.Contains(tree);
        }

        public IEnumerator<SyntaxTree> GetEnumerator()
        {
            return _trees.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _trees.GetEnumerator();
        }
    }
}
