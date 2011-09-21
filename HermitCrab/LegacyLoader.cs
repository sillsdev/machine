using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
    public class ConfigNode
    {
        public enum NodeType {COMMAND, OBJECT};

        public static ConfigNode ToConfigNode(object obj)
        {
            return obj as ConfigNode;
        }

        PatternNodeType m_type;
        string m_name;
        Dictionary<string, object> m_parameters;
		LegacyLoader m_loader;

        public ConfigNode(PatternNodeType type, string name, LegacyLoader loader)
        {
            m_type = type;
            m_name = name;
            m_parameters = new Dictionary<string, object>();
			m_loader = loader;
        }

        public PatternNodeType Type
        {
            get
            {
                return m_type;
            }
        }

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public void Add(string key, object value)
        {
            m_parameters[key] = value;
        }

        public List<string> GetStringList(string key)
        {
            return Get<List<object>>(key).ConvertAll<string>(Convert.ToString);
        }

        public bool GetStringList(string key, out List<string> value)
        {
            List<object> objList;
            if (Get<List<object>>(key, out objList))
            {
                value = objList.ConvertAll<string>(Convert.ToString);
                return true;
            }

            value = null;
            return false;
        }

        public List<ConfigNode> GetNodeList(string key)
        {
            return Get<List<object>>(key).ConvertAll<ConfigNode>(ToConfigNode);
        }

        public bool GetNodeList(string key, out List<ConfigNode> value)
        {
            List<object> objList;
            if (Get<List<object>>(key, out objList))
            {
                value = objList.ConvertAll<ConfigNode>(ToConfigNode);
                return true;
            }

            value = null;
            return false;
        }

        public T Get<T>(string key)
        {
            T value;
            if (!Get<T>(key, out value))
                throw new LoadException(LoadException.LoadErrorType.InvalidFormat, m_loader,
					string.Format(HCStrings.kstidFieldNotDefined, key, m_name, (m_type == PatternNodeType.COMMAND ? "command" : "object")));
            return value;
        }

        public bool Get<T>(string key, out T value)
        {
            object valObj;
            if (m_parameters.TryGetValue(key, out valObj))
            {
				if (!(valObj is T))
				{
					throw new LoadException(LoadException.LoadErrorType.InvalidFormat, m_loader,
						string.Format(HCStrings.kstidInvalidField, key, m_name, (m_type == PatternNodeType.COMMAND ? "command" : "object")));
				}
                value = (T) valObj;
                return true;
            }

            value = default(T);
            return false;
        }
    }

    /// <summary>
    /// This class parses the legacy HC input format in to a set of <see cref="ConfigNode"/> objects.
    /// </summary>
    public class LegacyParser
    {
        Regex m_spaceRegex;
		LegacyLoader m_loader;

        public LegacyParser(LegacyLoader loader)
        {
            m_spaceRegex = new Regex("\\s+");
			m_loader = loader;
        }

        public ICollection<ConfigNode> Parse(string configFile)
        {
            List<ConfigNode> nodes = new List<ConfigNode>();
            StreamReader r = null;
            try
            {
                r = new StreamReader(new FileStream(configFile, FileMode.Open, FileAccess.Read), Encoding.GetEncoding(1252));

                char[] parens = new char[] { '(', ')', '<', '>' };
                StringBuilder sb = new StringBuilder();
                int count = 0;
                char open = '(';
                char close = ')';
                while (!r.EndOfStream)
                {
                    string line = r.ReadLine().Trim();

                    if (string.IsNullOrEmpty(line) || line.StartsWith(";"))
                        continue;

                    int startIndex = 0;

                    int index = 0;
                    while ((index = line.IndexOfAny(parens, index)) != -1)
                    {
                        if (count == 0 && (line[index] == '(' || line[index] == '<'))
                        {
                            if (line[index] == '(')
                            {
                                open = '(';
                                close = ')';
                            }
                            else
                            {
                                open = '<';
                                close = '>';
                            }
                            startIndex = index + 1;
                        }

                        if (line[index] == open)
                        {
                            count++;
                        }
                        else if (line[index] == close)
                        {
                            count--;

                            if (count == 0)
                            {
                                sb.Append(line.Substring(startIndex, index - startIndex));

                                string str = m_spaceRegex.Replace(sb.ToString(), " ").Trim();
                                if (open == '(')
                                    nodes.Add(ParseCommand(str));
                                else
                                    nodes.Add(ParseObject(str));
                                sb = new StringBuilder();
                            }
                        }
                        index++;
                    }

                    if (count > 0)
                    {
                        sb.Append(line.Substring(startIndex, line.Length - startIndex));
                        sb.Append('\n');
                    }

                }
            }
            finally
            {
                if (r != null)
                    r.Close();
            }

            return nodes;
        }

        ConfigNode ParseCommand(string cmdStr)
        {
            int index = cmdStr.IndexOf(' ');
            string name = cmdStr.Substring(0, index);
            index++;
            ConfigNode cmd = new ConfigNode(ConfigNode.NodeType.COMMAND, name, m_loader);

            bool message = false;
            bool prettyPrint = false;
            if (cmdStr.IndexOf("message") == index)
            {
                message = true;
                index += 8;
            }
            else if (cmdStr.IndexOf("pretty_print") == index)
            {
                prettyPrint = true;
                index += 13;
            }

            cmd.Add("message", message);
            cmd.Add("pretty_print", prettyPrint);
            cmd.Add("param", ParseParameter(cmdStr, index, out index)); ;

            return cmd;
        }

        ConfigNode ParseObject(string objStr)
        {
            int begin = objStr.IndexOf(' ');
            string name = objStr.Substring(0, begin);
            begin++;
            ConfigNode obj = new ConfigNode(ConfigNode.NodeType.OBJECT, name, m_loader);

            int end;
            while (begin < objStr.Length && (end = objStr.IndexOf(' ', begin)) != -1)
            {
                string key = objStr.Substring(begin, end - begin);
                obj.Add(key, ParseParameter(objStr, end + 1, out begin));
                begin++;
            }

            return obj;
        }

        List<object> ParseList(string listStr)
        {
            List<object> list = new List<object>();
            int index = 0;
            while (index < listStr.Length)
            {
                list.Add(ParseParameter(listStr, index, out index));
                if (index < listStr.Length && listStr[index] == ' ')
                    index++;
            }

            return list;
        }

        int GetCloseIndex(string str, int index, char open, char close)
        {
            char[] delims = new char[2];
            delims[0] = open;
            delims[1] = close;

            int count = 0;
            while ((index = str.IndexOfAny(delims, index)) != -1)
            {
                if (str[index] == open)
                {
                    count++;
                }
                else if (str[index] == close)
                {
                    count--;

                    if (count == 0)
                    {
                        return index;
                    }
                }
                index++;
            }

            return -1;
        }

        object ParseParameter(string paramStr, int index, out int outIndex)
        {
            switch (paramStr[index])
            {
                case '<':
                    {
                        int closeIndex = GetCloseIndex(paramStr, index, '<', '>');
                        if (closeIndex >= 0)
                        {
                            outIndex = closeIndex + 1;
                            return ParseObject(paramStr.Substring(index + 1, closeIndex - (index + 1)).Trim());
                        }
                        break;
                    }

                case '(':
                    {
                        int closeIndex = GetCloseIndex(paramStr, index, '(', ')');
                        if (closeIndex >= 0)
                        {
                            outIndex = closeIndex + 1;
                            return ParseList(paramStr.Substring(index + 1, closeIndex - (index + 1)).Trim());
                        }
                        break;
                    }

                case '\x1F':
                case '\'':
                    {
                        int closeIndex = paramStr.IndexOf(paramStr[index], index + 1);
                        if (closeIndex >= 0)
                        {
                            outIndex = closeIndex + 1;
                            return paramStr.Substring(index + 1, closeIndex - (index + 1));
                        }
                        break;
                    }
            }

            outIndex = paramStr.IndexOf(' ', index);
            if (outIndex == -1)
                outIndex = paramStr.Length;
            return paramStr.Substring(index, outIndex - index);
        }
    }

    /// <summary>
    /// This class represents the loader for the legacy HC input format.
    /// </summary>
    public class LegacyLoader : Loader
    {
        static Stratum.PRuleOrder GetPRuleOrder(string ruleOrderStr)
        {
            switch (ruleOrderStr)
            {
                case "linear":
                    return Stratum.PRuleOrder.Linear;

                case "simultaneous":
                    return Stratum.PRuleOrder.Simultaneous;
            }

            return Stratum.PRuleOrder.Linear;
        }

        static Stratum.MRuleOrder GetMRuleOrder(string ruleOrderStr)
        {
            switch (ruleOrderStr)
            {
                case "linear":
                    return Stratum.MRuleOrder.Linear;

                case "unordered":
                    return Stratum.MRuleOrder.Unordered;
            }

            return Stratum.MRuleOrder.Unordered;
        }

        static StandardPhonologicalRule.MultAppOrder GetMultAppOrder(string multAppOrderStr)
        {
            switch (multAppOrderStr)
            {
                case "simultaneous":
                    return StandardPhonologicalRule.MultAppOrder.Simultaneous;

                case "rl_iterative":
                    return StandardPhonologicalRule.MultAppOrder.RightToLeftIterative;

                case "lr_iterative":
                    return StandardPhonologicalRule.MultAppOrder.LeftToRightIterative;
            }

            return StandardPhonologicalRule.MultAppOrder.LeftToRightIterative;
        }

        LegacyParser m_parser;
        string m_rootPath = null;

        public LegacyLoader()
        {
            m_parser = new LegacyParser(this);
        }

		public override Encoding DefaultOutputEncoding
		{
			get
			{
				return Encoding.GetEncoding(1252);
			}
		}

		public override void Load()
		{
			throw new NotImplementedException();
		}

        public override void Load(string configFile)
        {
            Reset();
            m_rootPath = Path.GetDirectoryName(configFile);
            LoadConfigNodes(m_parser.Parse(configFile));
            m_isLoaded = true;
        }

        public override void Reset()
        {
            base.Reset();
            m_rootPath = null;
        }

        void LoadConfigNodes(ICollection<ConfigNode> nodes)
        {
            foreach (ConfigNode node in nodes)
            {
                try
                {
                    switch (node.Type)
                    {
                        case ConfigNode.NodeType.COMMAND:
                            LoadCommand(node);
                            break;

                        case ConfigNode.NodeType.OBJECT:
                            LoadObject(node);
                            break;
                    }
                }
                catch (MorphException me)
                {
					if (_output != null)
						_output.Write(me);
                    if (m_quitOnError)
                        throw me;
                }
                catch (LoadException le)
                {
					if (_output != null)
						_output.Write(le);
                    if (m_quitOnError)
                        throw le;
                }
            }
        }

        void LoadCommand(ConfigNode cmd)
        {
            bool message = cmd.Get<bool>("message");

            switch (cmd.Name)
            {
                case "open_language":
                    string language = cmd.Get<string>("param");
                    _curMorpher = new Morpher(language, language);
                    m_morphers.Add(_curMorpher);
                    break;

                case "morpher_set":
                    List<object> list = cmd.Get<List<object>>("param");
                    switch (list[0] as string)
                    {
                        case "*pfeatures*":
                            LoadFeatureSystem(list[1] as List<object>);
                            break;

                        case "*strata*":
                            CheckCurMorpher();
                            _curMorpher.ClearStrata();
                            List<string> strataList = (list[1] as List<object>).ConvertAll<string>(Convert.ToString);
                            foreach (string stratumName in strataList)
                                _curMorpher.AddStratum(new Stratum(stratumName, stratumName, _curMorpher));
                            _curMorpher.AddStratum(new Stratum(Stratum.SurfaceStratumID, Stratum.SurfaceStratumID, _curMorpher));
                            break;
                        
                        case "*quit_on_error*":
                            m_quitOnError = (list[1] as string) == "true";
                            break;

                        case "*del_re_apps*":
                            CheckCurMorpher();
                            _curMorpher.DelReapplications = Convert.ToInt32(list[1] as string);
                            break;

                        case "*trace_inputs*":
                            m_traceInputs = (list[1] as string) == "true";
                            break;
                    }
                    break;

                case "set_stratum":
                    SetStratumSetting(cmd.Get<ConfigNode>("param"));
                    break;

                case "load_char_def_table":
                case "load_nat_class":
                case "load_morpher_rule":
                    LoadObject(cmd.Get<ConfigNode>("param"));
                    break;

                case "merge_in_dictionary_file":
                case "morpher_input_from_file":
                    string path = cmd.Get<string>("param");
                    if (!Path.IsPathRooted(path))
                    {
                        path = Path.Combine(m_rootPath, path);
                    }
                    ICollection<ConfigNode> newNodes = m_parser.Parse(path);
                    LoadConfigNodes(newNodes);
                    break;

                case "morph_and_lookup_word":
                    string word = cmd.Get<string>("param");
                    bool prettyPrint = cmd.Get<bool>("pretty_print");
                    CheckCurMorpher();
                    MorphAndLookupWord(word, prettyPrint);
                    break;

                case "remove_morpher_rule":
                    CheckCurMorpher();
                    string name = cmd.Get<string>("param");
                    _curMorpher.RemovePhonologicalRule(name);
                    _curMorpher.RemoveMorphologicalRule(name);
                    foreach (Stratum stratum in _curMorpher.Strata)
                    {
                        stratum.RemovePhonologicalRule(name);
                        stratum.RemoveMorphologicalRule(name);
                    }
                    break;

                case "remove_nat_class":
                    CheckCurMorpher();
                    _curMorpher.RemoveNaturalClass(cmd.Get<string>("param"));
                    break;

                case "del_char_def_table":
                    CheckCurMorpher();
                    _curMorpher.RemoveCharacterDefinitionTable(cmd.Get<string>("param"));
                    break;

                case "close_language":
                    CheckCurMorpher();
                    m_morphers.Remove(_curMorpher.ID);
                    _curMorpher = null;
                    break;

                case "assign_default_morpher_feature_value":
                    CheckCurMorpher();
                    List<object> feat = cmd.Get<List<object>>("param");
                    string featName = feat[0] as string;
                    Feature feature = _curMorpher.HeadFeatureSystem.GetFeature(featName);
                    if (feature == null)
                    {
                        feature = _curMorpher.FootFeatureSystem.GetFeature(featName);
                        if (feature == null)
                        {
                            feature = new Feature(featName, featName);
                            _curMorpher.HeadFeatureSystem.AddFeature(feature);
                            _curMorpher.FootFeatureSystem.AddFeature(feature);
                        }
                    }

                    List<string> values = ((List<object>) feat[1]).ConvertAll<string>(Convert.ToString);
                    var featVals = new IDBearerSet<FeatureSymbol>();
                    foreach (string value in values)
                    {
                        var val = new FeatureSymbol(value, value);
                        feature.AddPossibleValue(val);
                        featVals.Add(val);
                    }
                    feature.DefaultValue = new SimpleFeatureValue(featVals);
                    break;

                case "trace_morpher_rule":
                    CheckCurMorpher();
                    List<string> traceRuleParams = cmd.GetStringList("param");
                    bool traceAnalysis = traceRuleParams[0] == "true";
                    bool traceSynthesis = traceRuleParams[1] == "true";
                    if (traceRuleParams.Count == 3)
                        _curMorpher.SetTraceRule(traceRuleParams[2], traceAnalysis, traceSynthesis);
                    else
                        _curMorpher.SetTraceRules(traceAnalysis, traceSynthesis);
                    break;

                case "trace_morpher_strata":
                    CheckCurMorpher();
                    List<string> traceStrataParams = cmd.GetStringList("param");
                    _curMorpher.TraceStrataAnalysis = traceStrataParams[0] == "true";
                    _curMorpher.TraceStrataSynthesis = traceStrataParams[1] == "true";
                    break;

                case "trace_morpher_templates":
                    CheckCurMorpher();
                    List<string> traceTemplatesParams = cmd.GetStringList("param");
                    _curMorpher.TraceTemplatesAnalysis = traceTemplatesParams[0] == "true";
                    _curMorpher.TraceTemplatesSynthesis = traceTemplatesParams[1] == "true";
                    break;

                case "trace_lexical_lookup":
                    CheckCurMorpher();
                    bool traceLexLookup = false;
                    string traceLexLookupStr;
                    if (cmd.Get<string>("param", out traceLexLookupStr))
                        traceLexLookup = traceLexLookupStr == "true";
                    _curMorpher.TraceLexLookup = traceLexLookup;
                    break;

                case "trace_blocking":
                    CheckCurMorpher();
                    bool traceBlocking = false;
                    string traceBlockingStr;
                    if (cmd.Get<string>("param", out traceBlockingStr))
                        traceBlocking = traceBlockingStr == "true";
                    _curMorpher.TraceBlocking = traceBlocking;
                    break;
            }
        }

        void LoadObject(ConfigNode obj)
        {
            switch (obj.Name)
            {
                case "char_table":
                    LoadCharDefTable(obj);
                    break;

                case "nat_class":
                    LoadNaturalClass(obj);
                    break;

                case "prule":
                    LoadPRule(obj);
                    break;

                case "mrule":
                    LoadMRule(obj);
                    break;

                case "rz_rule":
                    LoadRealRule(obj);
                    break;

                case "comp_rule":
                    LoadCompRule(obj);
                    break;

                case "lex":
                    LoadLexEntry(obj);
                    break;
            }
        }

        void LoadFeatureSystem(List<object> featureList)
        {
            CheckCurMorpher();
            _curMorpher.PhoneticFeatureSystem.Reset();
            IEnumerator<object> enumerator = featureList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string featureName = enumerator.Current as string;
                Feature feature = new Feature(featureName, featureName);
                enumerator.MoveNext();
                foreach (object valueObj in (List<object>) enumerator.Current)
                {
                    string valueName = valueObj as string;
                    var value = new FeatureSymbol(featureName + valueName, valueName);
                    feature.AddPossibleValue(value);
					try
					{
						_curMorpher.PhoneticFeatureSystem.AddSymbol(value);
					}
					catch (InvalidOperationException ioe)
					{
						throw new LoadException(LoadException.LoadErrorType.TooManyFeatureValues, this,
							HCStrings.kstidTooManyFeatValues, ioe);
					}
                }
                _curMorpher.PhoneticFeatureSystem.AddFeature(feature);
            }
        }

        void LoadCharDefTable(ConfigNode charDefTableSpec)
        {
            CheckCurMorpher();
            string name = charDefTableSpec.Get<string>("name");
            CharacterDefinitionTable charDefTable = _curMorpher.GetCharacterDefinitionTable(name);
            if (charDefTable == null)
            {
#if IPA_CHAR_DEF_TABLE
                if (name == "*ipa*")
                    charDefTable = new IPACharacterDefinitionTable(name, name, m_curMorpher);
                else
                    charDefTable = new CharacterDefinitionTable(name, name, m_curMorpher);
#else
                charDefTable = new CharacterDefinitionTable(name, name, _curMorpher);
#endif
                _curMorpher.AddCharacterDefinitionTable(charDefTable);
            }

            charDefTable.Reset();
            charDefTable.Encoding = charDefTableSpec.Get<string>("encoding");

            List<object> segDefs = charDefTableSpec.Get<List<object>>("seg_defs");
            foreach (object obj in segDefs)
            {
                List<object> segDef = obj as List<object>;
                string strRep = segDef[0] as string;
                charDefTable.AddSegmentDefinition(strRep, LoadFeatValues((segDef[1] as List<object>)));
            }

            List<string> bdryDefs;
            if (charDefTableSpec.GetStringList("bdry_defs", out bdryDefs))
            {
                foreach (string def in bdryDefs)
                {
                    charDefTable.AddBoundaryDefinition(def);
                }
            }
        }

        void SetStratumSetting(ConfigNode stratumSetting)
        {
            CheckCurMorpher();
            string stratumName = stratumSetting.Get<string>("nm");
            if (stratumName == "*surface*")
                stratumName = Stratum.SurfaceStratumID;
            Stratum stratum = _curMorpher.GetStratum(stratumName);
            if (stratum == null)
                throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownStratum, stratumName), stratumName);
            switch (stratumSetting.Get<string>("type"))
            {
                case "ctable":
                    string tableName = stratumSetting.Get<string>("value");
                    stratum.CharacterDefinitionTable = GetCharDefTable(tableName);
                    break;

                case "cyclicity":
                    string cyclic = stratumSetting.Get<string>("value");
                    stratum.IsCyclic = cyclic == "cyclic";
                    break;

                case "prule":
                    string pruleOrder = stratumSetting.Get<string>("value");
                    stratum.PhonologicalRuleOrder = GetPRuleOrder(pruleOrder);
                    break;

                case "mrule":
                    string mruleOrder = stratumSetting.Get<string>("value");
                    stratum.MorphologicalRuleOrder = GetMRuleOrder(mruleOrder);
                    break;

                case "templates":
                    List<ConfigNode> tempSpecs = stratumSetting.GetNodeList("value");
                    foreach (AffixTemplate template in stratum.AffixTemplates)
                        _curMorpher.RemoveAffixTemplate(template.ID);
                    stratum.ClearAffixTemplates();
                    foreach (ConfigNode tempSpec in tempSpecs)
                    {
                        AffixTemplate template = LoadAffixTemplate(tempSpec);
                        stratum.AddAffixTemplate(template);
                        _curMorpher.AddAffixTemplate(template);
                    }
                    break;

            }
        }

        ICollection<FeatureSymbol> LoadFeatValues(List<object> mappingList)
        {
            var featVals = new List<FeatureSymbol>();
            IEnumerator<object> enumerator = mappingList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string featName = enumerator.Current as string;
                Feature feature = _curMorpher.PhoneticFeatureSystem.GetFeature(featName);
                if (feature == null)
                    throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeat, featName), featName);
                enumerator.MoveNext();
                string valueName = enumerator.Current as string;
                FeatureSymbol value = feature.GetPossibleValue(featName + valueName);
                if (value == null)
                    throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeatValue, valueName, featName), valueName);
                featVals.Add(value);
            }

            return featVals;
        }

        AlphaVariables LoadVarFeats(List<object> mappingList)
        {
            IDictionary<string, Feature> varFeats = new Dictionary<string, Feature>();
            if (mappingList != null)
            {
                IEnumerator<object> enumerator = mappingList.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string varName = enumerator.Current as string;
                    enumerator.MoveNext();
                    string featId = enumerator.Current as string;
                    Feature feature = _curMorpher.PhoneticFeatureSystem.GetFeature(featId);
                    if (feature == null)
                        throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownFeat, featId), featId);
                    varFeats[varName] = feature;
                }
            }

            return new AlphaVariables(varFeats);
        }

        void LoadNaturalClass(ConfigNode natClassSpec)
        {
            CheckCurMorpher();
            string name = natClassSpec.Get<string>("name");
            NaturalClass natClass = _curMorpher.GetNaturalClass(name);
            if (natClass == null)
            {
                natClass = new NaturalClass(name, name, _curMorpher);
                _curMorpher.AddNaturalClass(natClass);
            }

            ICollection<FeatureValue> featVals = LoadFeatValues(natClassSpec.Get<List<object>>("features"));
            natClass.FeatureStruct = new FeatureBundle(featVals, _curMorpher.PhoneticFeatureSystem);
        }

        void LoadPRule(ConfigNode pruleSpec)
        {
            CheckCurMorpher();
            string name = pruleSpec.Get<string>("nm");
            StandardPhonologicalRule rule = null;
            StandardPhonologicalRule prule = _curMorpher.GetPhonologicalRule(name);
            if (prule != null)
            {
                rule = prule as StandardPhonologicalRule;
            }
            else
            {
                rule = new StandardPhonologicalRule(name, name, _curMorpher);
                _curMorpher.AddPhonologicalRule(rule);
            }
            rule.Reset();

            string multAppOrderStr;
            if (pruleSpec.Get<string>("mult_applic", out multAppOrderStr))
                rule.MultApplication = GetMultAppOrder(multAppOrderStr);

            List<object> varFeatsList;
            pruleSpec.Get<List<object>>("var_fs", out varFeatsList);
            rule.AlphaVariables = LoadVarFeats(varFeatsList);

            rule.Lhs = new Pattern(true);
            LoadPSeq(rule.Lhs, pruleSpec.GetNodeList("in_pseq"), rule.AlphaVariables);

            List<ConfigNode> subruleList = pruleSpec.GetNodeList("subrules");
            foreach (ConfigNode srSpec in subruleList)
                LoadPSubrule(srSpec, rule);

            List<string> strata = pruleSpec.GetStringList("str");
            foreach (Stratum stratum in _curMorpher.Strata)
            {
                if (strata.Contains(stratum.ID))
                    stratum.AddPhonologicalRule(rule);
                else
                    stratum.RemovePhonologicalRule(name);
            }
        }

        void LoadPSubrule(ConfigNode psubruleSpec, StandardPhonologicalRule rule)
        {
            Pattern outSeq = new Pattern(true);
            LoadPSeq(outSeq, psubruleSpec.GetNodeList("out_pseq"), rule.AlphaVariables);

            Pattern leftEnv = null;
            ConfigNode leftEnvSpec;
            if (psubruleSpec.Get<ConfigNode>("left_environ", out leftEnvSpec))
                leftEnv = LoadPTemp(leftEnvSpec, rule.AlphaVariables);

            Pattern rightEnv = null;
            ConfigNode rightEnvSpec;
            if (psubruleSpec.Get<ConfigNode>("right_environ", out rightEnvSpec))
                rightEnv = LoadPTemp(rightEnvSpec, rule.AlphaVariables);

			StandardPhonologicalRule.Subrule sr = null;
			try
			{
				sr = new StandardPhonologicalRule.Subrule(outSeq, new Environment(leftEnv, rightEnv), rule);
			}
			catch (ArgumentException ae)
			{
				LoadException le = new LoadException(LoadException.LoadErrorType.InvalidSubruleType, this,
					HCStrings.kstidInvalidSubruleType, ae);
				le.Data["rule"] = rule.ID;
				throw le;
			}

            List<object> reqPOSs;
            if (!psubruleSpec.Get<List<object>>("r_pos", out reqPOSs))
            {
                reqPOSs = new List<object>();
            }
            sr.RequiredPartsOfSpeech = reqPOSs.ConvertAll<PartOfSpeech>(ToPOS);

            List<object> exFeats;
            if (!psubruleSpec.Get<List<object>>("x_rf", out exFeats))
            {
                exFeats = new List<object>();
            }
            sr.ExcludedMprFeatures = LoadMPRFeatures(exFeats);

            List<object> reqFeats;
            if (!psubruleSpec.Get<List<object>>("r_rf", out reqFeats))
            {
                reqFeats = new List<object>();
            }
            sr.RequiredMprFeatures = LoadMPRFeatures(reqFeats);

            rule.AddSubrule(sr);
        }

        Pattern LoadPTemp(ConfigNode ptempSpec, AlphaVariables varFeats)
        {
            bool initial = false;
            string initStr;
            if (ptempSpec.Get<string>("init", out initStr))
                initial = initStr == "true";

            bool final = false;
            string finStr;
            if (ptempSpec.Get<string>("fin", out finStr))
                final = finStr == "true";

            Pattern pattern = new Pattern();
            if (initial)
                pattern.Add(new MarginContext(Direction.LEFT));
            LoadPSeq(pattern, ptempSpec.GetNodeList("pseq"), varFeats);
            if (final)
                pattern.Add(new MarginContext(Direction.RIGHT));
            return pattern;
        }

        SimpleContext LoadNatClassCtxt(ConfigNode ctxtSpec, AlphaVariables varFeats)
        {
            string classStr = ctxtSpec.Get<string>("class");
            NaturalClass natClass = _curMorpher.GetNaturalClass(classStr);
            if (natClass == null)
                throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownNatClass, classStr), classStr);

            Dictionary<string, bool> vars = new Dictionary<string, bool>();
            List<string> varsList;
            if (ctxtSpec.GetStringList("alpha_vars", out varsList))
            {
                IEnumerator<string> enumerator = varsList.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string key = enumerator.Current;
                    enumerator.MoveNext();
                    bool polarity = enumerator.Current == "+";
                    vars.Add(key, polarity);
                }
            }
            return new NaturalClassContext(natClass, vars, varFeats);
        }

        void LoadLexEntry(ConfigNode entrySpec)
        {
            CheckCurMorpher();
            string id = entrySpec.Get<string>("id");
            string shapeStr = entrySpec.Get<string>("sh");
            LexEntry entry = new LexEntry(id, shapeStr, _curMorpher);

            string glossStr;
            if (entrySpec.Get<string>("gl", out glossStr))
                entry.Gloss = new Gloss(glossStr, glossStr, _curMorpher);
            entry.PartOfSpeech = ToPOS(entrySpec.Get<string>("pos"));
            string stratumName = entrySpec.Get<string>("str");
            Stratum stratum = _curMorpher.GetStratum(stratumName);
            if (stratum == null)
                throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownLexEntryStratum, id, stratumName), stratumName);
            entry.Stratum = stratum;
            PhoneticShape pshape = stratum.CharacterDefinitionTable.ToPhoneticShape(shapeStr, ModeType.SYNTHESIS);
			if (pshape == null)
			{
				LoadException le = new LoadException(LoadException.LoadErrorType.InvalidEntryShape, this,
					string.Format(HCStrings.kstidInvalidLexEntryShape, shapeStr, id, stratum.CharacterDefinitionTable.ID));
				le.Data["shape"] = shapeStr;
				le.Data["charDefTable"] = stratum.CharacterDefinitionTable.ID;
				le.Data["entry"] = entry.ID;
				throw le;
			}
            LexEntry.RootAllomorph allomorph = new LexEntry.RootAllomorph(id + "_allo", shapeStr, _curMorpher, pshape);
            entry.AddAllomorph(allomorph);
            _curMorpher.AddAllomorph(allomorph);
            List<object> mprFeats;
            if (!entrySpec.Get<List<object>>("rf", out mprFeats))
            {
                mprFeats = new List<object>();
            }
            entry.MprFeatures = LoadMPRFeatures(mprFeats);

            FeatureValues headFeats = null;
            List<object> headFeatsList;
            if (entrySpec.Get<List<object>>("hf", out headFeatsList))
                headFeats = LoadSynFeats(headFeatsList, _curMorpher.HeadFeatureSystem);
            else
                headFeats = new FeatureValues();
            entry.HeadFeatures = headFeats;

            FeatureValues footFeats = null;
            List<object> footFeatsList;
            if (entrySpec.Get<List<object>>("ff", out footFeatsList))
                footFeats = LoadSynFeats(footFeatsList, _curMorpher.FootFeatureSystem);
            else
                footFeats = new FeatureValues();
            entry.FootFeatures = footFeats;

            stratum.AddEntry(entry);
            string familyStr;
            if (entrySpec.Get<string>("fam", out familyStr))
            {
                LexFamily family = _curMorpher.Lexicon.GetFamily(familyStr);
                if (family == null)
                {
                    family = new LexFamily(familyStr, familyStr, _curMorpher);
                    _curMorpher.Lexicon.AddFamily(family);
                }
                family.AddEntry(entry);
            }
            _curMorpher.Lexicon.AddEntry(entry);
        }

        MprFeatureSet LoadMPRFeatures(List<object> mprFeatsList)
        {
            MprFeatureSet mprFeats = new MprFeatureSet();
            foreach (object mprFeatObj in mprFeatsList)
                mprFeats.Add(ToMPRFeature(mprFeatObj));
            return mprFeats;
        }

        FeatureValues LoadSynFeats(List<object> featsList, FeatureSystem featSys)
        {
            FeatureValues fv = new FeatureValues();
            IEnumerator<object> enumerator = featsList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string featureName = enumerator.Current as string;
                Feature feature = featSys.GetFeature(featureName);
                if (feature == null)
                {
                    feature = new Feature(featureName, featureName, _curMorpher);
                    featSys.AddFeature(feature);
                }

                enumerator.MoveNext();
                List<string> values = (enumerator.Current as List<object>).ConvertAll<string>(Convert.ToString);
                IDBearerSet<FeatureValue> featVals = new IDBearerSet<FeatureValue>();
                foreach (string valueName in values)
                {
                    string valueId = featureName + valueName;
                    FeatureValue value = featSys.GetValue(valueId);
                    if (value == null)
                    {
                        value = new FeatureValue(valueId, valueName, _curMorpher);
						try
						{
							featSys.AddValue(value);
						}
						catch (InvalidOperationException ioe)
						{
							throw new LoadException(LoadException.LoadErrorType.TooManyFeatureValues, this,
								HCStrings.kstidTooManyFeatValues, ioe);
						}
                    }

                    if (value.Feature != feature)
                    {
                        if (value.Feature != null)
                            value.Feature.RemovePossibleValue(valueId);
                        feature.AddPossibleValue(value);
                    }

                    featVals.Add(value);
                }
                fv.Add(feature, new ClosedValueInstance(featVals));
            }
            return fv;
        }

        void LoadPSeq(Pattern pattern, List<ConfigNode> seqList, AlphaVariables varFeats)
        {
            foreach (ConfigNode spec in seqList)
            {
                switch (spec.Name)
                {
                    case "simp_cntxt":
                        pattern.Add(LoadNatClassCtxt(spec, varFeats));
                        break;

                    case "bdry":
                        pattern.Add(LoadBdryCtxt(spec));
                        break;

                    case "opt_seq":
                        pattern.Add(LoadOptSeq(spec, varFeats, pattern.IsTarget));
                        break;

                    case "seg":
                        pattern.Add(LoadSegCtxt(spec));
                        break;
                }
            }
        }

        SimpleContext LoadSegCtxt(ConfigNode segSpec)
        {
            string strRep = segSpec.Get<string>("rep");
            string ctableName = segSpec.Get<string>("ctable");
            CharacterDefinitionTable charDefTable = GetCharDefTable(ctableName);
            SegmentDefinition segDef = charDefTable.GetSegmentDefinition(strRep);
            if (segDef == null)
                throw CreateUndefinedObjectException(string.Format(HCStrings.kstidInvalidRuleSeg, strRep, ctableName), strRep);
            return new SegmentContext(segDef);
        }

        BoundaryContext LoadBdryCtxt(ConfigNode bdrySpec)
        {
            string strRep = bdrySpec.Get<string>("rep");
            string ctableName = bdrySpec.Get<string>("ctable");
            CharacterDefinitionTable charDefTable = GetCharDefTable(ctableName);
            BoundaryDefinition bdryDef = charDefTable.GetBoundaryDefinition(strRep);
            return new BoundaryContext(bdryDef);
        }

        NestedPhoneticPattern LoadOptSeq(ConfigNode optSeqSpec, AlphaVariables varFeats, bool isTarget)
        {
            string minStr = optSeqSpec.Get<string>("min");
            string maxStr = optSeqSpec.Get<string>("max");
            int min = Convert.ToInt32(minStr);
            int max = Convert.ToInt32(maxStr);

            Pattern pattern = new Pattern(isTarget);
            LoadPSeq(pattern, optSeqSpec.GetNodeList("seq"), varFeats);
            return new NestedPhoneticPattern(pattern, min, max);
        }

        void LoadMRule(ConfigNode mruleSpec)
        {
            CheckCurMorpher();
            string name = mruleSpec.Get<string>("nm");
            AffixalMorphologicalRule rule = null;
            MorphologicalRule mrule = _curMorpher.GetMorphologicalRule(name);
            if (mrule != null)
            {
                rule = mrule as AffixalMorphologicalRule;
                foreach (AffixalMorphologicalRule.Subrule sr in rule.Subrules)
                    _curMorpher.RemoveAllomorph(sr.ID);
            }
            else
            {
                rule = new AffixalMorphologicalRule(name, name, _curMorpher);
                _curMorpher.AddMorphologicalRule(rule);
            }
            rule.Reset();

            string glossStr;
            if (mruleSpec.Get<string>("gl", out glossStr))
                rule.Gloss = new Gloss(glossStr, glossStr, _curMorpher);

            List<object> reqPOSs;
            if (!mruleSpec.Get<List<object>>("r_pos", out reqPOSs))
            {
                reqPOSs = new List<object>();
            }
            rule.RequiredPOSs = reqPOSs.ConvertAll<PartOfSpeech>(ToPOS);

            PartOfSpeech outPOS = null;
            string outPOSStr;
            if (mruleSpec.Get<string>("pos", out outPOSStr))
                outPOS = ToPOS(outPOSStr);
            rule.OutPOS = outPOS;

            List<object> reqHeadFeatsList;
            if (mruleSpec.Get<List<object>>("r_hf", out reqHeadFeatsList))
                rule.RequiredHeadFeatures = LoadSynFeats(reqHeadFeatsList, _curMorpher.HeadFeatureSystem);
            else
                rule.RequiredHeadFeatures = new FeatureValues();

            List<object> reqFootFeatsList;
            if (mruleSpec.Get<List<object>>("r_ff", out reqFootFeatsList))
                rule.RequiredFootFeatures = LoadSynFeats(reqFootFeatsList, _curMorpher.FootFeatureSystem);
            else
                rule.RequiredFootFeatures = new FeatureValues();

            List<object> outHeadFeatsList;
            if (mruleSpec.Get<List<object>>("hf", out outHeadFeatsList))
                rule.OutHeadFeatures = LoadSynFeats(outHeadFeatsList, _curMorpher.HeadFeatureSystem);
            else
                rule.OutHeadFeatures = new FeatureValues();

            List<object> outFootFeatsList;
            if (mruleSpec.Get<List<object>>("ff", out outFootFeatsList))
                rule.OutFootFeatures = LoadSynFeats(outFootFeatsList, _curMorpher.FootFeatureSystem);
            else
                rule.OutFootFeatures = new FeatureValues();

            List<string> obligFeatsList;
            IDBearerSet<Feature> obligFeats = new IDBearerSet<Feature>();
            if (mruleSpec.GetStringList("of", out obligFeatsList))
            {
                foreach (string obligFeat in obligFeatsList)
                {
                    Feature feature = _curMorpher.HeadFeatureSystem.GetFeature(obligFeat);
                    if (feature == null)
                    {
                        feature = new Feature(obligFeat, obligFeat, _curMorpher);
                        _curMorpher.HeadFeatureSystem.AddFeature(feature);
                    }
                    obligFeats.Add(feature);
                }
            }
            rule.ObligatoryHeadFeatures = obligFeats;

            bool blockable = true;
            string blockableStr;
            if (mruleSpec.Get<string>("blockable", out blockableStr))
                blockable = blockableStr == "true";
            rule.IsBlockable = blockable;

            List<ConfigNode> subruleList = mruleSpec.GetNodeList("subrules");
            foreach (ConfigNode srSpec in subruleList)
                LoadMSubrule(srSpec, rule);

            string stratumName;
            if (!mruleSpec.Get<string>("stratum", out stratumName))
            {
                stratumName = mruleSpec.Get<string>("str");
            }
            foreach (Stratum stratum in _curMorpher.Strata)
            {
                if (stratumName == stratum.ID)
                    stratum.AddMorphologicalRule(rule);
                else
                    stratum.RemoveMorphologicalRule(name);
            }
        }

        void LoadRealRule(ConfigNode realRuleSpec)
        {
            CheckCurMorpher();
            string name = realRuleSpec.Get<string>("nm");
            RealizationalRule rule = null;
            MorphologicalRule mrule = _curMorpher.GetMorphologicalRule(name);
            if (mrule != null)
            {
                rule = mrule as RealizationalRule;
                foreach (RealizationalRule.Subrule sr in rule.Subrules)
                    _curMorpher.RemoveAllomorph(sr.ID);
            }
            else
            {
                rule = new RealizationalRule(name, name, _curMorpher);
                _curMorpher.AddMorphologicalRule(rule);
            }
            rule.Reset();

            string glossStr;
            if (realRuleSpec.Get<string>("gl", out glossStr))
                rule.Gloss = new Gloss(glossStr, glossStr, _curMorpher);

            List<object> reqHeadFeatsList;
            if (realRuleSpec.Get<List<object>>("r_hf", out reqHeadFeatsList))
                rule.RequiredHeadFeatures = LoadSynFeats(reqHeadFeatsList, _curMorpher.HeadFeatureSystem);
            else
                rule.RequiredHeadFeatures = new FeatureValues();

            List<object> reqFootFeatsList;
            if (realRuleSpec.Get<List<object>>("r_ff", out reqFootFeatsList))
                rule.RequiredFootFeatures = LoadSynFeats(reqFootFeatsList, _curMorpher.FootFeatureSystem);
            else
                rule.RequiredFootFeatures = new FeatureValues();

            List<object> realFeatsList;
            if (realRuleSpec.Get<List<object>>("rz_f", out realFeatsList))
                rule.RealizationalFeatures = LoadSynFeats(realFeatsList, _curMorpher.HeadFeatureSystem);
            else
                rule.RealizationalFeatures = new FeatureValues();

            bool blockable = true;
            string blockableStr;
            if (realRuleSpec.Get<string>("blockable", out blockableStr))
                blockable = blockableStr == "true";
            rule.IsBlockable = blockable;

            List<ConfigNode> subruleList = realRuleSpec.GetNodeList("subrules");
            foreach (ConfigNode srSpec in subruleList)
                LoadMSubrule(srSpec, rule);
        }

        void LoadMSubrule(ConfigNode msubruleSpec, AffixalMorphologicalRule rule)
        {
            List<object> varFeatsList;
            msubruleSpec.Get<List<object>>("var_fs", out varFeatsList);
            AlphaVariables varFeatures = LoadVarFeats(varFeatsList);

            ConfigNode lhs = msubruleSpec.Get<ConfigNode>("in");
            List<Pattern> lhsList = new List<Pattern>();
            List<object> pseqList = lhs.Get<List<object>>("pseq");
            for (int i = 0; i < pseqList.Count; i++)
            {
                Pattern pattern = new Pattern();
                LoadPSeq(pattern, (pseqList[i] as List<object>).ConvertAll<ConfigNode>(ConfigNode.ToConfigNode),
                    varFeatures);
                lhsList.Add(pattern);
            }

            ConfigNode rhs = msubruleSpec.Get<ConfigNode>("out");
            List<object> poutList = rhs.Get<List<object>>("p_out");
            List<MorphologicalOutput> rhsList = LoadPhonOutput(poutList, varFeatures, rule.ID);

            string id = rule.ID + "_subrule" + rule.SubruleCount;
            AffixalMorphologicalRule.Subrule sr = new AffixalMorphologicalRule.Subrule(id, id, _curMorpher,
                lhsList, rhsList, varFeatures, MorphologicalTransform.RedupMorphType.IMPLICIT);

            List<object> exFeats;
            if (!lhs.Get<List<object>>("x_rf", out exFeats))
            {
                exFeats = new List<object>();
            }
            sr.ExcludedMPRFeatures = LoadMPRFeatures(exFeats);

            List<object> reqFeats;
            if (!lhs.Get<List<object>>("r_rf", out reqFeats))
            {
                reqFeats = new List<object>();
            }
            sr.RequiredMPRFeatures = LoadMPRFeatures(reqFeats);

            List<object> outFeats;
            if (!rhs.Get<List<object>>("rf", out outFeats))
            {
                outFeats = new List<object>();
            }
            sr.OutputMPRFeatures = LoadMPRFeatures(outFeats);

            rule.AddSubrule(sr);
            _curMorpher.AddAllomorph(sr);
        }

        void LoadCompRule(ConfigNode compRuleSpec)
        {
            CheckCurMorpher();
            string name = compRuleSpec.Get<string>("nm");
            CompoundingRule rule = null;
            MorphologicalRule mrule = _curMorpher.GetMorphologicalRule(name);
            if (mrule != null)
            {
                rule = mrule as CompoundingRule;
                foreach (CompoundingRule.Subrule sr in rule.Subrules)
                    _curMorpher.RemoveAllomorph(sr.ID);
            }
            else
            {
                rule = new CompoundingRule(name, name, _curMorpher);
                _curMorpher.AddMorphologicalRule(rule);
            }
            rule.Reset();

            string glossStr;
            if (compRuleSpec.Get<string>("gl", out glossStr))
                rule.Gloss = new Gloss(glossStr, glossStr, _curMorpher);

            List<object> headPOSs;
            if (!compRuleSpec.Get<List<object>>("head_pos", out headPOSs))
            {
                headPOSs = new List<object>();
            }
            rule.HeadRequiredPOSs = headPOSs.ConvertAll<PartOfSpeech>(ToPOS);

            List<object> nonHeadPOSs;
            if (!compRuleSpec.Get<List<object>>("nonhead_pos", out nonHeadPOSs))
            {
                nonHeadPOSs = new List<object>();
            }
            rule.NonHeadRequiredPOSs = nonHeadPOSs.ConvertAll<PartOfSpeech>(ToPOS);

            PartOfSpeech outPOS = null;
            string outPOSStr;
            if (compRuleSpec.Get<string>("pos", out outPOSStr))
                outPOS = ToPOS(outPOSStr);
            rule.OutPOS = outPOS;

            List<object> headReqHeadFeatsList;
            if (compRuleSpec.Get<List<object>>("head_r_hf", out headReqHeadFeatsList))
                rule.HeadRequiredHeadFeatures = LoadSynFeats(headReqHeadFeatsList, _curMorpher.HeadFeatureSystem);
            else
                rule.HeadRequiredHeadFeatures = new FeatureValues();

            List<object> headReqFootFeatsList;
            if (compRuleSpec.Get<List<object>>("head_r_ff", out headReqFootFeatsList))
                rule.HeadRequiredFootFeatures = LoadSynFeats(headReqFootFeatsList, _curMorpher.FootFeatureSystem);
            else
                rule.HeadRequiredFootFeatures = new FeatureValues();

            List<object> nonHeadReqHeadFeatsList;
            if (compRuleSpec.Get<List<object>>("nonhead_r_hf", out nonHeadReqHeadFeatsList))
                rule.NonHeadRequiredHeadFeatures = LoadSynFeats(nonHeadReqHeadFeatsList, _curMorpher.HeadFeatureSystem);
            else
                rule.NonHeadRequiredHeadFeatures = new FeatureValues();

            List<object> nonHeadReqFootFeatsList;
            if (compRuleSpec.Get<List<object>>("nonhead_r_ff", out nonHeadReqFootFeatsList))
                rule.NonHeadRequiredFootFeatures = LoadSynFeats(nonHeadReqFootFeatsList, _curMorpher.FootFeatureSystem);
            else
                rule.NonHeadRequiredFootFeatures = new FeatureValues();

            List<object> outHeadFeatsList;
            if (compRuleSpec.Get<List<object>>("hf", out outHeadFeatsList))
                rule.OutHeadFeatures = LoadSynFeats(outHeadFeatsList, _curMorpher.HeadFeatureSystem);
            else
                rule.OutHeadFeatures = new FeatureValues();

            List<object> outFootFeatsList;
            if (compRuleSpec.Get<List<object>>("ff", out outFootFeatsList))
                rule.OutFootFeatures = LoadSynFeats(outFootFeatsList, _curMorpher.FootFeatureSystem);
            else
                rule.OutFootFeatures = new FeatureValues();

            List<string> obligFeatsList;
            IDBearerSet<Feature> obligFeats = new IDBearerSet<Feature>();
            if (compRuleSpec.GetStringList("of", out obligFeatsList))
            {
                foreach (string obligFeat in obligFeatsList)
                {
                    Feature feature = _curMorpher.HeadFeatureSystem.GetFeature(obligFeat);
                    if (feature == null)
                    {
                        feature = new Feature(obligFeat, obligFeat, _curMorpher);
                        _curMorpher.HeadFeatureSystem.AddFeature(feature);
                    }
                    obligFeats.Add(feature);
                }
            }
            rule.ObligatoryHeadFeatures = obligFeats;

            bool blockable = true;
            string blockableStr;
            if (compRuleSpec.Get<string>("blockable", out blockableStr))
                blockable = blockableStr == "true";
            rule.IsBlockable = blockable;

            List<ConfigNode> subruleList = compRuleSpec.GetNodeList("subrules");
            foreach (ConfigNode srSpec in subruleList)
                LoadCompSubrule(srSpec, rule);

            string stratumName;
            if (!compRuleSpec.Get<string>("stratum", out stratumName))
            {
                stratumName = compRuleSpec.Get<string>("str");
            }
            foreach (Stratum stratum in _curMorpher.Strata)
            {
                if (stratumName == stratum.ID)
                    stratum.AddMorphologicalRule(rule);
                else
                    stratum.RemoveMorphologicalRule(name);
            }
        }

        void LoadCompSubrule(ConfigNode compSubruleSpec, CompoundingRule rule)
        {
            List<object> varFeatsList;
            compSubruleSpec.Get<List<object>>("var_fs", out varFeatsList);
            AlphaVariables varFeatures = LoadVarFeats(varFeatsList);

            ConfigNode headLhs = compSubruleSpec.Get<ConfigNode>("head");
            List<Pattern> headLhsList = new List<Pattern>();
            List<object> headPseqList = headLhs.Get<List<object>>("pseq");
            for (int i = 0; i < headPseqList.Count; i++)
            {
                Pattern pattern = new Pattern();
                LoadPSeq(pattern, (headPseqList[i] as List<object>).ConvertAll<ConfigNode>(ConfigNode.ToConfigNode),
                    varFeatures);
                headLhsList.Add(pattern);
            }

            ConfigNode nonHeadLhs = compSubruleSpec.Get<ConfigNode>("nonhead");
            List<Pattern> nonHeadLhsList = new List<Pattern>();
            List<object> nonHeadPseqList = nonHeadLhs.Get<List<object>>("pseq");
            for (int i = 0; i < nonHeadPseqList.Count; i++)
            {
                Pattern pattern = new Pattern();
                LoadPSeq(pattern, (nonHeadPseqList[i] as List<object>).ConvertAll<ConfigNode>(ConfigNode.ToConfigNode),
                    varFeatures);
                nonHeadLhsList.Add(pattern);
            }

            ConfigNode rhs = compSubruleSpec.Get<ConfigNode>("out");
            List<object> poutList = rhs.Get<List<object>>("p_out");
            List<MorphologicalOutput> rhsList = LoadPhonOutput(poutList, varFeatures, rule.ID);

            string id = rule.ID + "_subrule" + rule.SubruleCount;
            CompoundingRule.Subrule sr = new CompoundingRule.Subrule(id, id, _curMorpher,
                headLhsList, nonHeadLhsList, rhsList, varFeatures);

            List<object> exFeats;
            if (!headLhs.Get<List<object>>("x_rf", out exFeats))
            {
                exFeats = new List<object>();
            }
            sr.ExcludedMPRFeatures = LoadMPRFeatures(exFeats);

            List<object> reqFeats;
            if (!headLhs.Get<List<object>>("r_rf", out reqFeats))
            {
                reqFeats = new List<object>();
            }
            sr.RequiredMPRFeatures = LoadMPRFeatures(reqFeats);

            List<object> outFeats;
            if (!rhs.Get<List<object>>("rf", out outFeats))
            {
                outFeats = new List<object>();
            }
            sr.OutputMPRFeatures = LoadMPRFeatures(outFeats);

            rule.AddSubrule(sr);
            _curMorpher.AddAllomorph(sr);
        }

        List<MorphologicalOutput> LoadPhonOutput(List<object> poutList, AlphaVariables varFeatures, string ruleName)
        {
            List<MorphologicalOutput> rhsList = new List<MorphologicalOutput>();
            foreach (object poutObj in poutList)
            {
                if (poutObj is string)
                {
                    int partition = Convert.ToInt32(poutObj as string) - 1;
                    rhsList.Add(new CopyFromInput(partition));
                }
                else if (poutObj is ConfigNode)
                {
                    SimpleContext ctxt = LoadNatClassCtxt(poutObj as ConfigNode, varFeatures);
                    rhsList.Add(new InsertSimpleContext(ctxt));
                }
                else
                {
                    IList<object> list = poutObj as IList<object>;
                    if (list[1] is ConfigNode)
                    {
                        int partition = Convert.ToInt32(list[0] as string) - 1;
                        SimpleContext ctxt = LoadNatClassCtxt(list[1] as ConfigNode, varFeatures);
                        rhsList.Add(new ModifyFromInput(partition, ctxt, _curMorpher));
                    }
                    else
                    {
                        string shapeStr = list[0] as string;
                        string ctableName = list[1] as string;
                        CharacterDefinitionTable charDefTable = GetCharDefTable(ctableName);
                        PhoneticShape pshape = charDefTable.ToPhoneticShape(shapeStr, ModeType.SYNTHESIS);
						if (pshape == null)
						{
							LoadException le = new LoadException(LoadException.LoadErrorType.InvalidRuleShape, this,
								string.Format(HCStrings.kstidInvalidRuleShape, shapeStr, ruleName, ctableName));
							le.Data["shape"] = shapeStr;
							le.Data["charDefTable"] = charDefTable.ID;
							le.Data["rule"] = ruleName;
							throw le;
						}
                        rhsList.Add(new InsertSegments(pshape));
                    }
                }
            }
            return rhsList;
        }

        AffixTemplate LoadAffixTemplate(ConfigNode tempSpec)
        {
            CheckCurMorpher();
            string name = tempSpec.Get<string>("nm");
            AffixTemplate template = new AffixTemplate(name, name, _curMorpher);

            List<object> reqPOSs;
            if (!tempSpec.Get<List<object>>("r_pos", out reqPOSs))
            {
                reqPOSs = new List<object>();
            }
            template.RequiredPartsOfSpeech = reqPOSs.ConvertAll<PartOfSpeech>(ToPOS);

            List<object> slots = tempSpec.Get<List<object>>("slots");
            for (int i = 0; i < slots.Count; i++)
            {
                string slotName = null;
                if (slots[i] is string)
                {
					if ((slots[i++] as string) != "name")
					{
						throw new LoadException(LoadException.LoadErrorType.InvalidFormat, this,
							string.Format(HCStrings.kstidInvalidSlot, name));
					}
                    slotName = slots[i++] as string;
                }
                else
                {
                    slotName = name + i;
                }
                Slot slot = new Slot(slotName, slotName, _curMorpher);
                List<string> rules = (slots[i] as List<object>).ConvertAll<string>(Convert.ToString);
                RealizationalRule lastRule = null;
                foreach (string ruleId in rules)
                {
                    RealizationalRule rule = _curMorpher.GetMorphologicalRule(ruleId) as RealizationalRule;
                    slot.AddRule(rule);
                    lastRule = rule;
                }
                slot.IsOptional = lastRule.RealizationalFeatures.NumFeatures > 0;
                template.AddSlot(slot);
            }

            return template;
        }

        void CheckCurMorpher()
        {
            if (_curMorpher == null)
                throw new LoadException(LoadException.LoadErrorType.NoCurrentMorpher, this, HCStrings.kstidNoLang);
        }

        MprFeature ToMPRFeature(object value)
        {
            string name = value as string;
            MprFeature mprFeat = _curMorpher.GetMprFeature(name);
            if (mprFeat == null)
            {
                mprFeat = new MprFeature(name, name, _curMorpher);
                _curMorpher.AddMprFeature(mprFeat);
            }
            return mprFeat;
        }

        PartOfSpeech ToPOS(object value)
        {
            string name = value as string;
            PartOfSpeech pos = _curMorpher.GetPartOfSpeech(name);
            if (pos == null)
            {
                pos = new PartOfSpeech(name, name, _curMorpher);
                _curMorpher.AddPartOfSpeech(pos);
            }

            return pos;
        }

        CharacterDefinitionTable GetCharDefTable(string name)
        {
            CharacterDefinitionTable charDefTable = _curMorpher.GetCharacterDefinitionTable(name);
            if (charDefTable == null)
                throw CreateUndefinedObjectException(string.Format(HCStrings.kstidUnknownCharDefTable, name), name);
            return charDefTable;
        }
    }
}