// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MonoDevelop.MSBuild.Language.Expressions;
using MonoDevelop.MSBuild.Schema;
using MonoDevelop.MSBuild.Language.Typesystem;
using MonoDevelop.MSBuild.Language.Syntax;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.MSBuild.Language
{
	abstract class MSBuildResolvingVisitor : MSBuildVisitor
	{
		protected override void VisitElementValue (XElement element, MSBuildElementSyntax resolved, string value, int offset)
		{
			base.VisitElementValue (element, resolved, value, offset);

			var info = Document.GetSchemas ().GetElementInfo (resolved, (element.Parent as XElement)?.Name.Name, element.Name.Name, true);
			if (info == null) {
				return;
			}

			VisitValue (element, null, resolved, null, info, value, offset);
		}

		protected override void VisitResolvedAttribute (
			XElement element, XAttribute attribute,
			MSBuildElementSyntax resolvedElement, MSBuildAttributeSyntax resolvedAttribute)
		{
			VisitAttributeValue (element, attribute, resolvedElement, resolvedAttribute);
			base.VisitResolvedAttribute (element, attribute, resolvedElement, resolvedAttribute);
		}

		void VisitAttributeValue (
			XElement element, XAttribute attribute,
			MSBuildElementSyntax resolvedElement, MSBuildAttributeSyntax resolvedAttribute)
		{
			if (string.IsNullOrWhiteSpace (attribute.Value)) {
				return;
			}

			var info = Document.GetSchemas ().GetAttributeInfo (resolvedAttribute, element.Name.Name, attribute.Name.Name);

			if (info == null) {
				return;
			}

			VisitValue (
				element, attribute, resolvedElement, resolvedAttribute,
				info, attribute.Value, attribute.ValueOffset);
		}

		protected virtual void VisitValue (
			XElement element, XAttribute attribute,
			MSBuildElementSyntax resolvedElement, MSBuildAttributeSyntax resolvedAttribute,
			ITypedSymbol valueType, string value, int offset)
		{
			var kind = MSBuildCompletionExtensions.InferValueKindIfUnknown (valueType);

			if (!kind.AllowExpressions ()) {
				VisitValueExpression (
					element, attribute, resolvedElement, resolvedAttribute,
					valueType, kind, new ExpressionText (offset, value, true));
				return;
			}

			var expression =
				valueType?.ValueKind == MSBuildValueKind.Condition
					? ExpressionParser.ParseCondition (value, offset)
					: ExpressionParser.Parse (value, kind.GetExpressionOptions (), offset);

			VisitValueExpression (
				element, attribute, resolvedElement, resolvedAttribute,
				valueType, kind, expression);
		}

		protected virtual void VisitValueExpression (
			XElement element, XAttribute attribute,
			MSBuildElementSyntax resolvedElement, MSBuildAttributeSyntax resolvedAttribute,
			ITypedSymbol valueType, MSBuildValueKind kind, ExpressionNode node)
		{
		}
	}
}
