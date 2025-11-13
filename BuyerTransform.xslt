<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt"
    exclude-result-prefixes="msxsl">

    <!-- Output as HTML -->
    <xsl:output method="html" indent="yes" />

    <xsl:template match="BuyerData">
        <html>
            <head>
                <style>
                    table {
                        border-collapse: collapse;
                        margin: 10px 0;
                        width: 400px;
                    }
                    th, td {
                        border: 1px solid #333;
                        padding: 5px 8px;
                        text-align: left;
                    }
                    th {
                        background-color: #f2f2f2;
                    }
                    caption {
                        font-weight: bold;
                        text-align: left;
                        margin-bottom: 6px;
                    }
                </style>
            </head>
            <body>
                <h2>Buyer Information</h2>

                <table>
                    <caption>Basic Info</caption>
                    <tr>
                        <th>Name</th>
                        <td><xsl:value-of select="Name" /></td>
                    </tr>
                </table>

                <table>
                    <caption>Address Details</caption>
                    <xsl:call-template name="split-and-map">
                        <xsl:with-param name="text" select="Address" />
                        <xsl:with-param name="delimiter" select="', '" />
                        <xsl:with-param name="labels">streetAddress,city,postalCode,country</xsl:with-param>
                    </xsl:call-template>
                </table>

                <table>
                    <caption>Contact Details</caption>
                    <xsl:call-template name="split-and-map">
                        <xsl:with-param name="text" select="Contact" />
                        <xsl:with-param name="delimiter" select="' | '" />
                        <xsl:with-param name="labels">phone,email</xsl:with-param>
                    </xsl:call-template>
                </table>
            </body>
        </html>
    </xsl:template>

    <!-- Split logic -->
    <xsl:template name="split-and-map">
        <xsl:param name="text" />
        <xsl:param name="delimiter" />
        <xsl:param name="labels" />
        <xsl:param name="labelIndex" select="1" />

        <xsl:variable name="currentLabel">
            <xsl:call-template name="get-token">
                <xsl:with-param name="text" select="$labels" />
                <xsl:with-param name="delimiter" select="','" />
                <xsl:with-param name="index" select="$labelIndex" />
            </xsl:call-template>
        </xsl:variable>

        <xsl:choose>
            <xsl:when test="contains($text, $delimiter)">
                <xsl:variable name="token" select="normalize-space(substring-before($text, $delimiter))" />
                <xsl:if test="normalize-space($token) != '' and $currentLabel != ''">
                    <tr>
                        <th><xsl:value-of select="$currentLabel" /></th>
                        <td><xsl:value-of select="$token" /></td>
                    </tr>
                </xsl:if>

                <xsl:call-template name="split-and-map">
                    <xsl:with-param name="text" select="substring-after($text, $delimiter)" />
                    <xsl:with-param name="delimiter" select="$delimiter" />
                    <xsl:with-param name="labels" select="$labels" />
                    <xsl:with-param name="labelIndex" select="$labelIndex + 1" />
                </xsl:call-template>
            </xsl:when>

            <xsl:otherwise>
                <xsl:if test="normalize-space($text) != '' and $currentLabel != ''">
                    <tr>
                        <th><xsl:value-of select="$currentLabel" /></th>
                        <td><xsl:value-of select="normalize-space($text)" /></td>
                    </tr>
                </xsl:if>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <!-- Helper to get nth token -->
    <xsl:template name="get-token">
        <xsl:param name="text" />
        <xsl:param name="delimiter" />
        <xsl:param name="index" />
        <xsl:variable name="token" select="substring-before(concat($text, $delimiter), $delimiter)" />

        <xsl:choose>
            <xsl:when test="$index = 1">
                <xsl:value-of select="normalize-space($token)" />
            </xsl:when>
            <xsl:otherwise>
                <xsl:call-template name="get-token">
                    <xsl:with-param name="text" select="substring-after($text, $delimiter)" />
                    <xsl:with-param name="delimiter" select="$delimiter" />
                    <xsl:with-param name="index" select="$index - 1" />
                </xsl:call-template>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>
