﻿using System;
using System.Collections.Generic;

namespace Serilog.Sinks.Elasticsearch
{
    /// <summary>
    ///
    /// </summary>
    public enum AutoRegisterTemplateVersion
    {
        /// <summary>
        /// Elasticsearch version &lt;= 2.4
        /// </summary>
        ESv2 = 0,
        /// <summary>
        /// Elasticsearch version &lt;= version 5.6
        /// </summary>
        ESv5 = 1,
        /// <summary>
        /// Elasticsearch version &gt;= version 6.0
        /// </summary>
        ESv6 = 2
    }

    /// <summary>
    ///
    /// </summary>
    public class ElasticsearchTemplateProvider
    {
        ///  <summary>
        ///
        ///  </summary>
        ///  <param name="settings"></param>
        /// <param name="templateMatchString"></param>
        /// <param name="version"></param>
        ///  <returns></returns>
        public static object GetTemplate(
            Dictionary<string, string> settings,
            string templateMatchString,
            AutoRegisterTemplateVersion version = AutoRegisterTemplateVersion.ESv2)
        {
            switch (version)
            {
                case AutoRegisterTemplateVersion.ESv5:
                    return GetTemplateESv5(settings, templateMatchString);
                case AutoRegisterTemplateVersion.ESv6:
                    return GetTemplateESv6(settings, templateMatchString);
                case AutoRegisterTemplateVersion.ESv2:
                    return GetTemplateESv2(settings, templateMatchString);
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), version, null);
            }
        }

        private static object GetTemplateESv6(
            Dictionary<string, string> settings,
            string templateMatchString)
        {
            return new
            {
                index_patterns = new[] { templateMatchString },
                settings = settings,
                mappings = new
                {
                    _default_ = new
                    {
                        dynamic_templates = new List<Object>
                        {
                            //when you use serilog as an adaptor for third party frameworks
                            //where you have no control over the log message they typically
                            //contain {0} ad infinitum, we force numeric property names to
                            //contain strings by default.
                            {
                                new
                                {
                                    numerics_in_fields = new
                                    {
                                        path_match = @"fields\.[\d+]$",
                                        match_pattern = "regex",
                                        mapping = new
                                        {
                                            type = "text",
                                            index = true,
                                            norms = false
                                        }
                                    }
                                }
                            },
                            {
                                new
                                {
                                    string_fields = new
                                    {
                                        match = "*",
                                        match_mapping_type = "string",
                                        mapping = new
                                        {
                                            type = "text",
                                            index = true,
                                            norms = false,
                                            fields = new
                                            {
                                                raw = new
                                                {
                                                    type = "keyword",
                                                    index = true,
                                                    ignore_above = 256
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        properties = new Dictionary<string, object>
                        {
                            {"message", new {type = "text", index = "true"}},
                            {
                                "exceptions", new
                                {
                                    type = "nested",
                                    properties = new Dictionary<string, object>
                                    {
                                        {"Depth", new {type = "integer"}},
                                        {"RemoteStackIndex", new {type = "integer"}},
                                        {"HResult", new {type = "integer"}},
                                        {"StackTraceString", new {type = "text", index = "true"}},
                                        {"RemoteStackTraceString", new {type = "text", index = "true"}},
                                        {
                                            "ExceptionMessage", new
                                            {
                                                type = "object",
                                                properties = new Dictionary<string, object>
                                                {
                                                    {"MemberType", new {type = "integer"}},
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private static object GetTemplateESv5(
            Dictionary<string, string> settings,
            string templateMatchString)
        {
            return new
            {
                template = templateMatchString,
                settings = settings,
                mappings = new
                {
                    _default_ = new
                    {
                        _all = new { enabled = true, norms = false },
                        dynamic_templates = new List<Object>
                        {
                            //when you use serilog as an adaptor for third party frameworks
                            //where you have no control over the log message they typically
                            //contain {0} ad infinitum, we force numeric property names to
                            //contain strings by default.
                            {
                                new
                                {
                                    numerics_in_fields = new
                                    {
                                        path_match = @"fields\.[\d+]$",
                                        match_pattern = "regex",
                                        mapping = new
                                        {
                                            type = "text",
                                            index = true,
                                            norms = false
                                        }
                                    }
                                }
                            },
                            {
                                new
                                {
                                    string_fields = new
                                    {
                                        match = "*",
                                        match_mapping_type = "string",
                                        mapping = new
                                        {
                                            type = "text",
                                            index = true,
                                            norms = false,
                                            fields = new
                                            {
                                                raw = new
                                                {
                                                    type = "keyword",
                                                    index = true,
                                                    ignore_above = 256
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        properties = new Dictionary<string, object>
                        {
                            {"message", new {type = "text", index = "analyzed"}},
                            {
                                "exceptions", new
                                {
                                    type = "nested",
                                    properties = new Dictionary<string, object>
                                    {
                                        {"Depth", new {type = "integer"}},
                                        {"RemoteStackIndex", new {type = "integer"}},
                                        {"HResult", new {type = "integer"}},
                                        {"StackTraceString", new {type = "text", index = "analyzed"}},
                                        {"RemoteStackTraceString", new {type = "text", index = "analyzed"}},
                                        {
                                            "ExceptionMessage", new
                                            {
                                                type = "object",
                                                properties = new Dictionary<string, object>
                                                {
                                                    {"MemberType", new {type = "integer"}},
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private static object GetTemplateESv2(
            Dictionary<string, string> settings,
            string templateMatchString)
        {
            return new
            {
                template = templateMatchString,
                settings = settings,
                mappings = new
                {
                    _default_ = new
                    {
                        _all = new { enabled = true, omit_norms = true },
                        dynamic_templates = new List<Object>
                        {
                            //when you use serilog as an adaptor for third party frameworks
                            //where you have no control over the log message they typically
                            //contain {0} ad infinitum, we force numeric property names to
                            //contain strings by default.
                            {
                                new
                                {
                                    numerics_in_fields = new
                                    {
                                        path_match = @"fields\.[\d+]$",
                                        match_pattern = "regex",
                                        mapping = new
                                        {
                                            type = "string",
                                            index = "analyzed",
                                            omit_norms = true
                                        }
                                    }
                                }
                            },
                            {
                                new
                                {
                                    string_fields = new
                                    {
                                        match = "*",
                                        match_mapping_type = "string",
                                        mapping = new
                                        {
                                            type = "string",
                                            index = "analyzed",
                                            omit_norms = true,
                                            fields = new
                                            {
                                                raw = new
                                                {
                                                    type = "string",
                                                    index = "not_analyzed",
                                                    ignore_above = 256
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        properties = new Dictionary<string, object>
                        {
                            {"message", new {type = "string", index = "analyzed"}},
                            {
                                "exceptions", new
                                {
                                    type = "nested",
                                    properties = new Dictionary<string, object>
                                    {
                                        {"Depth", new {type = "integer"}},
                                        {"RemoteStackIndex", new {type = "integer"}},
                                        {"HResult", new {type = "integer"}},
                                        {"StackTraceString", new {type = "string", index = "analyzed"}},
                                        {"RemoteStackTraceString", new {type = "string", index = "analyzed"}},
                                        {
                                            "ExceptionMessage", new
                                            {
                                                type = "object",
                                                properties = new Dictionary<string, object>
                                                {
                                                    {"MemberType", new {type = "integer"}},
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
