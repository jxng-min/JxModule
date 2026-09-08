using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace JxModule.Terminal
{
    public sealed class JxTerminalBootstrapper : MonoBehaviour
    {
        [SerializeField] private JxTerminalGUI terminalGUI;
        [SerializeField] private JxTerminalCommandInstallerBehaviour[] installers;
        [SerializeField] private bool autoDiscoverInstallers;

        private bool _isInstalled;

        private void Awake()
        {
            Install();
        }

        public void Install()
        {
            if (_isInstalled)
            {
                return;
            }

            if (terminalGUI == null)
            {
                terminalGUI = GetComponent<JxTerminalGUI>();
            }

            if (terminalGUI == null)
            {
                Debug.LogError($"{nameof(JxTerminalBootstrapper)} requires {nameof(JxTerminalGUI)}.", this);
                return;
            }

            InstallInspectorInstallers();

            if (autoDiscoverInstallers)
            {
                InstallDiscoveredInstallers();
            }

            _isInstalled = true;
        }

        private void InstallInspectorInstallers()
        {
            if (installers == null)
            {
                return;
            }

            foreach (var installer in installers)
            {
                if (installer == null)
                {
                    continue;
                }

                installer.Install(terminalGUI.Terminal);
            }
        }

        private void InstallDiscoveredInstallers()
        {
            var ignoredTypes = GetInspectorInstallerTypes();
            foreach (var installer in CreateDiscoveredInstallers(ignoredTypes))
            {
                installer.Install(terminalGUI.Terminal);
            }
        }

        private HashSet<Type> GetInspectorInstallerTypes()
        {
            var ignoredTypes = new HashSet<Type>();
            if (installers == null)
            {
                return ignoredTypes;
            }

            foreach (var installer in installers)
            {
                if (installer == null)
                {
                    continue;
                }

                ignoredTypes.Add(installer.GetType());
            }

            return ignoredTypes;
        }

        private static IEnumerable<IJxTerminalCommandInstaller> CreateDiscoveredInstallers(HashSet<Type> ignoredTypes)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(GetTypes)
                .Where(type => IsDiscoverableInstaller(type, ignoredTypes))
                .Select(type => (IJxTerminalCommandInstaller)Activator.CreateInstance(type));
        }

        private static IEnumerable<Type> GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(type => type != null);
            }
        }

        private static bool IsDiscoverableInstaller(Type type, HashSet<Type> ignoredTypes)
        {
            return typeof(IJxTerminalCommandInstaller).IsAssignableFrom(type) &&
                   !ignoredTypes.Contains(type) &&
                   !typeof(MonoBehaviour).IsAssignableFrom(type) &&
                   !type.IsAbstract &&
                   !type.IsInterface &&
                   type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}
