using System.Diagnostics;
using System.Xml;
using Umbraco.Core.Resolving;

namespace Umbraco.Web.Routing
{
	/// <summary>
	/// Provides an implementation of <see cref="IDocumentLookup"/> that handles page nice urls.
	/// </summary>
	/// <remarks>
	/// <para>Handles <c>/foo/bar</c> where <c>/foo/bar</c> is the nice url of a document.</para>
	/// </remarks>
	[ResolutionWeight(10)]
    internal class LookupByNiceUrl : IDocumentLookup
    {
		static readonly TraceSource Trace = new TraceSource("LookupByNiceUrl");

		/// <summary>
		/// Tries to find and assign an Umbraco document to a <c>DocumentRequest</c>.
		/// </summary>
		/// <param name="docRequest">The <c>DocumentRequest</c>.</param>
		/// <returns>A value indicating whether an Umbraco document was found and assigned.</returns>
		public virtual bool TrySetDocument(DocumentRequest docreq)
        {
			string route;
			if (docreq.HasDomain)
				route = docreq.Domain.RootNodeId.ToString() + Domains.PathRelativeToDomain(docreq.DomainUri, docreq.Uri.AbsolutePath);
			else
				route = docreq.Uri.AbsolutePath;

            var node = LookupDocumentNode(docreq, route);
            return node != null;
        }

		/// <summary>
		/// Tries to find an Umbraco document for a <c>DocumentRequest</c> and a route.
		/// </summary>
		/// <param name="docreq">The document request.</param>
		/// <param name="route">The route.</param>
		/// <returns>The document node, or null.</returns>
        protected XmlNode LookupDocumentNode(DocumentRequest docreq, string route)
        {
            Trace.TraceInformation("Test route \"{0}\"", route);

			//return '0' if in preview mode!
        	var nodeId = !docreq.RoutingContext.UmbracoContext.InPreviewMode
							? docreq.RoutingContext.UmbracoContext.RoutesCache.GetNodeId(route)
        	             	: 0;


            XmlNode node = null;
            if (nodeId > 0)
            {
				node = docreq.RoutingContext.ContentStore.GetNodeById(nodeId);
                if (node != null)
                {
                    docreq.Node = node;
                    Trace.TraceInformation("Cache hit, id={0}", nodeId);
                }
                else
                {
                    docreq.RoutingContext.UmbracoContext.RoutesCache.ClearNode(nodeId);
                }
            }

            if (node == null)
            {
                Trace.TraceInformation("Cache miss, query");
				node = docreq.RoutingContext.ContentStore.GetNodeByRoute(route);
                if (node != null)
                {
                    docreq.Node = node;
                    Trace.TraceInformation("Query matches, id={0}", docreq.NodeId);

					if (!docreq.RoutingContext.UmbracoContext.InPreviewMode)
					{
						docreq.RoutingContext.UmbracoContext.RoutesCache.Store(docreq.NodeId, route); // will not write if previewing	
					} 
                    
                }
                else
                {
                    Trace.TraceInformation("Query does not match");
                }
            }

            return node;
        }
    }
}