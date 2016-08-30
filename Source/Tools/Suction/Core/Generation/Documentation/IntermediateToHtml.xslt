﻿<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl s c q"
                xmlns:s="cfs:documentation-1591" xmlns="http://www.w3.org/1999/xhtml"
                xmlns:h="http://www.w3.org/1999/xhtml"
                xmlns:c="ms:csdoc"
                xmlns:q="cfs:documentation-csharp-1596"
                xmlns:x="cfs:documentation-callback"
               
>
    <xsl:output method="html" indent="no"/>
  <xsl:preserve-space elements="span"/>


  <!-- embeded HTML -->
  <xsl:template match="h:a" >
    <xsl:choose  >
      <xsl:when test="@href">
        <!-- href attribute is present: use it -->
        <xsl:call-template name="TranslateReference" >
          <xsl:with-param name="cref">
            <xsl:value-of select="@href"/>
          </xsl:with-param>
          <xsl:with-param name="label">
            <xsl:apply-templates />
          </xsl:with-param>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <!-- href attribute NOT present: use content text -->
        <xsl:call-template name="TranslateReference" >
          <xsl:with-param name="cref">
            <xsl:value-of select="text()"/>
          </xsl:with-param>
          <xsl:with-param name="label">
            <xsl:apply-templates />
          </xsl:with-param>
        </xsl:call-template>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="@* | node()">
        <xsl:copy>
            <xsl:apply-templates select="@* | node()"/>
        </xsl:copy>
    </xsl:template>


  <!-- macros -->

  <xsl:template name="TranslateReference" >
    <xsl:param name="cref" />
    <xsl:param name="label" />
    <xsl:variable name="u" select="x:TranslateReference($cref)" />
    <xsl:choose>
      <xsl:when test="$u = '#unresolved'">
        <span class="code-unresolved">
          <xsl:value-of select="$label" />
        </span>
      </xsl:when>
      <xsl:otherwise>
        <a href="{$u}" >
          <xsl:value-of select="$label" />
        </a>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <!-- CSDOC comment language -->

  <xsl:template match="c:summary" >
    <p>
      <xsl:apply-templates />
    </p>
  </xsl:template>

  <xsl:template match="c:remarks" >
    <p>
      <xsl:apply-templates />
    </p>
  </xsl:template>

  <xsl:template match="c:para" >
    <p>
      <xsl:apply-templates />
    </p>
  </xsl:template>

  <xsl:template match="c:param" >
    <header>
      <xsl:value-of select="@name"/>
    </header>
    <p>
      <xsl:apply-templates />
    </p>
  </xsl:template>

  <xsl:template match="c:returns" >
    <p>
      <xsl:apply-templates />
    </p>
  </xsl:template>

  <xsl:template match="c:see" >
    <xsl:call-template name="TranslateReference" >
      <xsl:with-param name="cref">
        <xsl:value-of select="@cref"/>
      </xsl:with-param>
      <xsl:with-param name="label">
        <xsl:value-of select="@cref"/>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>
  
  <!-- code doc -->

  <xsl:template match="q:a" >
    <xsl:call-template name="TranslateReference" >
      <xsl:with-param name="cref">
        <xsl:value-of select="@href"/>
      </xsl:with-param>
      <xsl:with-param name="label">
        <xsl:apply-templates />
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <xsl:template match="q:code" >
    <xsl:choose>
      <xsl:when test="q:header" >
      <code class="expander expander-right">
        <xsl:apply-templates />
      </code>
      </xsl:when>
      <xsl:otherwise>
        <code>
          <xsl:apply-templates />
        </code>
      </xsl:otherwise>
    </xsl:choose>
    </xsl:template>

  <xsl:template match="q:header" >
    <div class="code-header">
      <xsl:apply-templates />
    </div>
  </xsl:template>

  <xsl:template match="q:implementation" >
    <pre class="code-implementation">
      <xsl:apply-templates />
    </pre>
  </xsl:template>
 

  <!-- types -->

  <xsl:template match="s:Identifier" >
    <xsl:call-template name="TranslateReference" >
      <xsl:with-param name="cref">
        <xsl:value-of select="@cref"/>
      </xsl:with-param>
      <xsl:with-param name="label">
        <xsl:value-of select="@Name"/>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <xsl:template match="s:Predefined" >
    <xsl:value-of select="@Name"/>
  </xsl:template>

  <xsl:template match="s:Array" >
    <span class="array">
      <xsl:apply-templates />[]
    </span>
  </xsl:template>

  <xsl:template match="s:Type" >
    <span class="type">
      <xsl:apply-templates />
    </span>
  </xsl:template>

  <xsl:template match="s:Modifier" >
    <xsl:value-of select="."/>
    <xsl:text> </xsl:text>
  </xsl:template>

  <xsl:template match="s:Declaration" >
    <section class="declaration" >
      <xsl:apply-templates />
    </section>
  </xsl:template>

  <!-- elements -->
  
  <xsl:template match="s:Property|s:Field|s:Event" >
    <section>
      <h1>
        <xsl:value-of select="@Name" />
      </h1>
      <xsl:apply-templates select="s:Annotation/c:summary" />
      <xsl:apply-templates select="s:Declaration" />
      <xsl:apply-templates select="s:Annotation/c:remarks" />
    </section>
  </xsl:template>

  <xsl:template name="ParametersAndReturns" >

    <xsl:if test="s:Annotation/c:param" >
      <section class="parameters" >
        <h1>Parameters</h1>
        <xsl:for-each select="s:Annotation/c:param" >
          <xsl:apply-templates select="." />
        </xsl:for-each>
      </section>
    </xsl:if>

    <xsl:if test="s:Annotation/c:returns" >
      <section class="parameters" >
        <h1>Return Value</h1>
        <xsl:for-each select="s:Annotation/c:returns" >
          <xsl:apply-templates select="." />
        </xsl:for-each>
      </section>
    </xsl:if>


  </xsl:template>

  <xsl:template match="s:Constructor" >
    <xsl:apply-templates select="s:Annotation/c:summary" />
    <xsl:apply-templates select="s:Declaration" />
    <xsl:apply-templates select="s:Annotation/c:remarks" />
  </xsl:template>

  <xsl:template match="s:Method" >
    <section class="method" >
      <h1>
        <xsl:value-of select="@Name" />
      </h1>
      <xsl:apply-templates select="s:Annotation/c:summary" />
      <xsl:apply-templates select="s:Declaration" />

      <xsl:call-template name="ParametersAndReturns" />
      <xsl:apply-templates select="s:Annotation/c:remarks" />
    </section>
  </xsl:template>

  <xsl:template match="s:Attribute" >
    <h3>
      <xsl:value-of select="@Name" />
    </h3>
    <xsl:apply-templates select="s:Annotation/c:summary" />
    <xsl:apply-templates select="s:Declaration" />
    <xsl:apply-templates select="s:Annotation/c:remarks" />
  </xsl:template>


  <xsl:template match="s:Annotation|s:Return" >
    <xsl:apply-templates />
  </xsl:template>

  <xsl:template match="s:TypeArguments" >
    <span>&lt;<xsl:for-each select="s:Type" >
      <xsl:apply-templates select="." />
      <xsl:if test="position() != last()" >
        <xsl:text>, </xsl:text>
      </xsl:if>
    </xsl:for-each>&gt;</span></xsl:template>

  <xsl:template match="s:TopicLink" >
    <xsl:call-template name="TranslateReference" >
      <xsl:with-param name="cref">
        <xsl:value-of select="@RefKey"/>
      </xsl:with-param>
      <xsl:with-param name="label">
        <xsl:value-of select="@Name"/>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>
  
 
  <!-- s:TypeDocumentation -->

  <xsl:template match="s:TypeDocumentation" >
    <h1>
      <xsl:value-of select="@Kind"/>
      <xsl:text> </xsl:text>
      <span class="type-name" >
        <xsl:value-of select="@Name"/>
      </span>
    </h1>

    <xsl:apply-templates select="s:Annotation/c:summary" />

    <xsl:apply-templates select="s:Declaration" />

    <xsl:if test="s:Annotation/c:remarks" >
      <section class="expander remarks">
        <h1>Remarks</h1>
        <xsl:apply-templates select="s:Annotation/c:remarks" />
      </section>
    </xsl:if>

    <xsl:if test="s:TopicLink[@Kind='TopicIsBaseClassOf']" >
      <section class="expander derived-classes">
        <h1>Derived Classes</h1>
        <ul class="ref-list" >
          <xsl:for-each select="s:TopicLink[@Kind='TopicIsBaseClassOf']" >
            <li>
              <xsl:apply-templates select="." />
            </li>
          </xsl:for-each>
        </ul>
      </section>
    </xsl:if>

    <xsl:if test="s:TopicLink[@Kind='TopicNamespaceContains']" >
      <section class="expander namespace-types">
        <h1>Contained Types</h1>
        <ul class="ref-list" >
          <xsl:for-each select="s:TopicLink[@Kind='TopicNamespaceContains']" >
            <li>
              <xsl:apply-templates select="." />
            </li>
          </xsl:for-each>
        </ul>
      </section>
    </xsl:if>

    <xsl:if test="s:Attribute" >
      <section class="expander attributes">
        <h1>Attributes</h1>
        <xsl:for-each select="s:Attribute" >
          <xsl:apply-templates select="." />
        </xsl:for-each>
      </section>
    </xsl:if>

    <xsl:if test="s:Constructor" >
      <section class="expander constructors">
        <h1>Constructors</h1>
        <xsl:for-each select="s:Constructor" >
          <xsl:sort select="@Name"/>
          <xsl:apply-templates select="." />
          <div class="code-spacer" >&#160;</div>
        </xsl:for-each>
      </section>
    </xsl:if>

    <xsl:if test="s:Field" >
      <section class="expander fields">
        <h1>Fields</h1>
        <table>
          <xsl:for-each select="s:Field" >
            <xsl:sort select="@Name"/>
            <xsl:apply-templates select="." />
          </xsl:for-each>
        </table>
      </section>
    </xsl:if>

    <xsl:if test="s:Event" >
      <section class="expander events">
        <h1>Events</h1>
        <table>
          <xsl:for-each select="s:Event" >
            <xsl:sort select="@Name"/>
            <xsl:apply-templates select="." />
          </xsl:for-each>
        </table>
      </section>
    </xsl:if>

    <xsl:if test="s:Property" >
      <section class="expander properties">
        <h1>Properties</h1>
        <table>
          <xsl:for-each select="s:Property" >
            <xsl:sort select="@Name"/>
            <xsl:apply-templates select="." />
          </xsl:for-each>
        </table>
      </section>
    </xsl:if>
    <xsl:if test="s:Method" >
      <section class="expander methods">
        <h1>Methods</h1>
        <xsl:for-each select="s:Method" >
          <xsl:sort select="@Name"/>
          <xsl:apply-templates select="." />
        </xsl:for-each>
      </section>
    </xsl:if>
  </xsl:template>
  
  <!-- ExternalDocumentation -->

  <xsl:template match="s:ExternalDocumentation" >
    <xsl:apply-templates select="s:Body" />
  </xsl:template>

  <xsl:template match="s:Body" >
    <xsl:apply-templates select="*" />
  </xsl:template>

  <!-- TopicReference -->

  <xsl:template match="s:TopicReference" >
    <a href="{@Key}.html" >
      <xsl:apply-templates />
    </a>
  </xsl:template>

  <xsl:template match="s:Index" >
    <h1>Index</h1>
    <ul>
      <xsl:for-each select="s:TopicReference" >
        <li>
          <xsl:apply-templates select="." />
        </li>
      </xsl:for-each>
    </ul>
  </xsl:template>

  <xsl:template match="s:Topic" >
    <section class="topic" key="{s:TypeDocumentation/@Key}" >
      <xsl:apply-templates />
    </section>
  </xsl:template>

  <xsl:template match="/" >
    <html>
      <head>
        <style>
          body { font-family: 'Segoe UI'; }
          table { border-spacing: 5px 0px; }
          .member-name { font-weight: bold; }
          .parameter-name { font-style: italic; }
          .extended { padding: 7px; }
          .code-identifier { font-weight: bold; }
          .code-syntax-unhandled { color: red; }
          .code-comment { color: green; }
          .code-header { border: 0px solid blue; margin: 0px; }
          .code-implementation { border: 0px solid blue; margin: 0px; }
          .code-unresolved { color: gray; }

          .docframe-header { padding: 10px; 
            background: rgba(0,0,255,0.05);
          }
          
          .code-spacer { height: 10px; }
        </style>

        <link rel="stylesheet" href="appstyle.css" />
        <link rel="stylesheet" href="docstyle.css" />
        <script src="docframe.js" > /* */ </script>
        <script src="cfs.decoration.js" > /* */ </script>
        <script>
          function bodyloaded() { DocumentationFrame.instance().setTopic(
        '<xsl:value-of select="s:Topic/@DocID" />'
        ); }</script>
        <meta name="cfs-docid" content="{s:Topic/@DocID}"/>
      </head>
      <body onload="bodyloaded()">
        <xsl:apply-templates />

        <script>
            DecorationManager.Install(document.body);
        </script>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
