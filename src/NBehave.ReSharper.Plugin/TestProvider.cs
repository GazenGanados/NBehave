﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.AttributeChecker;
using JetBrains.UI;

namespace NBehave.ReSharper.Plugin
{
    [UnitTestProvider]
    public class TestProvider : IUnitTestProvider
    {
        public const string NBehaveId = "NBehave";

        private readonly UnitTestElementComparer _unitTestElementComparer = new UnitTestElementComparer(new[]
                                                                                            {
	                                                                                            typeof(NBehaveScenarioTestElement), 
                                                                                            });

        private readonly ISolution _soultion;
        private readonly UnitTestAttributeCache _unitTestAttributeCache;
        private UnitTestManager _manager;

        private UnitTestManager Manager
        {
            get
            {
                if (_manager == null)
                    _manager = Solution.GetComponent<UnitTestManager>();
                return _manager;
            }
        }

        public TestProvider(ISolution solution, UnitTestAttributeCache unitTestAttributeCache)
        {
            _soultion = solution;
            _unitTestAttributeCache = unitTestAttributeCache;
        }

        public System.Drawing.Image Icon
        {
            get { return ImageLoader.GetImage("nbehave", new Assembly[0]); }
        }

        public ISolution Solution
        {
            get { return _soultion; }
        }

        public string ID
        {
            get
            {
                return NBehaveId;
            }
        }

        public string Name
        {
            get { return NBehaveId; }
        }

        public RemoteTaskRunnerInfo GetTaskRunnerInfo()
        {
            return new RemoteTaskRunnerInfo(typeof(NBehaveTaskRunner));
        }

        public bool IsElementOfKind(IDeclaredElement declaredElement, UnitTestElementKind elementKind)
        {
            switch (elementKind)
            {
                case UnitTestElementKind.Unknown:
                    {
                        return !IsUnitTestStuff(declaredElement);
                    }
                case UnitTestElementKind.Test:
                    {
                        return IsUnitTest(declaredElement);
                    }
                case UnitTestElementKind.TestContainer:
                    {
                        return IsUnitTestContainer(declaredElement);
                    }
                case UnitTestElementKind.TestStuff:
                    {
                        return IsUnitTestStuff(declaredElement);
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException("elementKind");
                    }
            }
        }

        public bool IsElementOfKind(IUnitTestElement element, UnitTestElementKind elementKind)
        {
            switch (elementKind)
            {
                case UnitTestElementKind.Unknown:
                    {
                        return !(element is NBehaveUnitTestElementBase);
                    }
                case UnitTestElementKind.Test:
                    {
                        return element is NBehaveScenarioTestElement;
                    }
                case UnitTestElementKind.TestContainer:
                    {
                        return element is NBehaveScenarioTestElement;
                    }
                case UnitTestElementKind.TestStuff:
                    {
                        return element is NBehaveUnitTestElementBase;
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException("elementKind");
                    }
            }
        }

        private bool IsUnitTest(IDeclaredElement element)
        {
            var typeMember = element as ITypeMember;
            return typeMember != null && MetadataExplorer.IsActionStepMethod(typeMember, _unitTestAttributeCache);
        }

        private bool IsUnitTestContainer(IDeclaredElement element)
        {
            var typeElement = element as ITypeElement;
            bool flag;
            return typeElement != null && MetadataExplorer.IsActionStepsClass(typeElement, out flag, _unitTestAttributeCache);
        }

        private bool IsUnitTestStuff(IDeclaredElement declaredElement)
        {
            return IsUnitTest(declaredElement)
                    || IsUnitTestContainer(declaredElement);
        }

        public bool IsSupported(IHostProvider hostProvider)
        {
            return true;
        }

        public void ExploreSolution(ISolution solution, UnitTestElementConsumer consumer)
        {
        }

        public void ExploreExternal(UnitTestElementConsumer consumer)
        {
        }

        public int CompareUnitTestElements(IUnitTestElement x, IUnitTestElement y)
        {
            return _unitTestElementComparer.Compare(x, y);
        }

        public void SerializeElement(XmlElement parent, IUnitTestElement element)
        { }

        public IUnitTestElement DeserializeElement(XmlElement parent, IUnitTestElement parentElement)
        {
            return null;
        }

        public IUnitTestElement GetOrCreateFixture(ClrTypeName id, IProject project, ProjectModelElementEnvoy projectEnvoy, string assemblyPath, NBehaveScenarioTestElement parent)
        {
            IUnitTestElement elementById = Manager.GetElementById(project, id.FullName);
            if (elementById != null)
            {
                if (parent != null)
                    elementById.Parent = parent;

                elementById.State = UnitTestElementState.Valid;
                RemoveInvalidNBehaveElements(elementById);
                var scenarioTestElement = elementById as NBehaveScenarioTestElement;
                //if (scenarioTestElement != null)
                //    scenarioTestElement.AssemblyLocation = assemblyPath;
                return scenarioTestElement;
            }
            //return new NBehaveScenarioTestElement(this, id.FullName, projectEnvoy, id);
            return null;
        }

        private void RemoveInvalidNBehaveElements(IUnitTestElement elementById)
        {
            List<IUnitTestElement> list = (elementById.Children.Where(child => child.State == UnitTestElementState.Invalid && child is NBehaveScenarioTestElement)).ToList();
            foreach (IUnitTestElement current in list)
            {
                current.Parent = null;
                Manager.RemoveElement(current);
            }
        }
    }
}
