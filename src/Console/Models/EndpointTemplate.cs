//-----------------------------------------------------------------------
// <copyright file="EndpointTemplate.cs" company="GardnerWebTech">
//    Copyright (c) GardnerWebTech. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotnetCqrsClassTemplatesUtility.Console.Models;

public record Template(string TemplatePath, string Name, string SolutionName, string ApiFactoryName = null);