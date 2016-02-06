﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Chummer.Skills;

namespace Chummer.UI.Shared
{
	public partial class SkillsTabUserControl : UserControl
	{
		public event PropertyChangedEventHandler ChildPropertyChanged; 

		private SkillsDisplay<Skill> _skills;
		private SkillsDisplay<SkillGroup> _groups;
		private SkillsDisplay<KnowledgeSkill> _knoSkills; 

		public SkillsTabUserControl()
		{
			InitializeComponent();

			//LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
		}

		private bool _loadCalled = false;
		private bool _initialized = false;
		private Character _character;
		private List<Tuple<string, Func<Skill, bool>>> _dropDownList;

		public Character ObjCharacter
		{
			set
			{
				_character = value;
				RealLoad();
			}
			get { return _character; }
		}
		private void SkillsTabUserControl_Load(object sender, EventArgs e)
		{
			_loadCalled = true;
			RealLoad();
		}

		private void RealLoad() //Ensure loaded and got character obj
		{
			if (_initialized) return;

			if (!(_character != null && _loadCalled)) return;

			_initialized = true;
			Stopwatch sw = Stopwatch.StartNew();
			Stopwatch parts = Stopwatch.StartNew();
			//Keep everything visible until ready to display everything. This 
			//seems to prevent redrawing everything each time anything is added
			//Not benched, but should be faster

			//Might also be useless horseshit, 2 lines

			//Visible = false;
			//this.SuspendLayout();
			MakeSkillDisplays();

			parts.TaskEnd("MakeSkillDisplay()");
			_dropDownList = GenerateDropdownFilter();

			parts.TaskEnd("GenerateDropDown()");

			cboDisplayFilter.DataSource = _dropDownList;
			cboDisplayFilter.ValueMember = "Item2";
			cboDisplayFilter.DisplayMember = "Item1";
			cboDisplayFilter.SelectedIndex = 0;
			cboDisplayFilter.MaxDropDownItems = _dropDownList.Count;

			parts.TaskEnd("_ddl databind");

			_skills.ChildPropertyChanged += ChildPropertyChanged;
			_groups.ChildPropertyChanged += ChildPropertyChanged;
			_knoSkills.ChildPropertyChanged += ChildPropertyChanged;

			//Visible = true;
			//this.ResumeLayout(false);
			//this.PerformLayout();
			parts.TaskEnd("visible");
			Panel1_Resize(null, null);
			Panel2_Resize(null, null);
			parts.TaskEnd("resize");
			sw.Stop();
			Debug.WriteLine("RealLoad() in {0} ms", sw.Elapsed.TotalMilliseconds);
			//this.Update();
			//this.ResumeLayout(true);
			//this.PerformLayout();
		}

		private List<Tuple<string, Func<Skill, bool>>> GenerateDropdownFilter()
		{
			List<Tuple<string, Func<Skill, bool>>> ret = new List<Tuple<string, Func<Skill, bool>>>
			{
				new Tuple<string, Func<Skill, bool>>(LanguageManager.Instance.GetString("String_SkillFilterAll"), skill => true),
				new Tuple<string, Func<Skill, bool>>(LanguageManager.Instance.GetString("String_SkillFilterRatingAboveZero"),
					skill => skill.Rating > 0),
				new Tuple<string, Func<Skill, bool>>(LanguageManager.Instance.GetString("String_SkillFilterTotalRatingAboveZero"),
					skill => skill.Pool > 0),
				new Tuple<string, Func<Skill, bool>>(LanguageManager.Instance.GetString("String_SkillFilterRatingZero"),
					skill => skill.Rating == 0)
			};
			//TODO: TRANSLATIONS



			ret.AddRange(
				from XmlNode objNode 
				in XmlManager.Instance.Load("skills.xml").SelectNodes("/chummer/categories/category[@type = \"active\"]")
				let displayName = objNode.Attributes["translate"]?.InnerText ?? objNode.InnerText
				select new Tuple<string, Func<Skill, bool>>(
					$"{LanguageManager.Instance.GetString("Label_Category")} {displayName}", 
					skill => skill.SkillCategory == objNode.InnerText));

			ret.AddRange(
				from string attribute
				in new[]{"BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL", "MAG", "RES"} //TODO: This should be somewhere in Character or CharacterAttrib i think
				select new Tuple<string, Func<Skill, bool>>(
					$"{LanguageManager.Instance.GetString("String_ExpenseAttribute")}: {LanguageManager.Instance.GetString($"String_Attribute{attribute}Short")}",
					skill => skill.Attribute == attribute));

			ret.AddRange(
				from SkillGroup @group
				in _character.SkillGroups
				select new Tuple<string, Func<Skill, bool>>(
					$"{LanguageManager.Instance.GetString("String_ExpenseSkillGroup")}: {@group.DisplayName}", 
					skill => skill.SkillGroupObject == @group));

			return ret;
		}

		private void MakeSkillDisplays()
		{
			Stopwatch sw = Stopwatch.StartNew();
			_groups = new SkillsDisplay<SkillGroup>(_character.SkillGroups, @group => new SkillGroupControl(@group))
			{
				Location = new Point(0, 15),
			};
			sw.TaskEnd("_groups");

			splitSkills.Panel1.Controls.Add(_groups);

			sw.TaskEnd("_group add");

			_skills = new SkillsDisplay<Skill>(_character.Skills, skill => new SkillControl2(skill))
			{
				Location = new Point(265, 39),
			};
			
			sw.TaskEnd("_skills");

			splitSkills.Panel1.Controls.Add(_skills);

			sw.TaskEnd("_skills add");

			_knoSkills = new SkillsDisplay<KnowledgeSkill>(_character.KnowledgeSkills,
				knoSkill => new KnowledgeSkillControl(knoSkill))
			{
				Location = new Point(3, 50),
			};

			splitSkills.Panel2.Controls.Add(_knoSkills);
			
		}


		private void Panel1_Resize(object sender, EventArgs e)
		{
			int height = splitSkills.Panel1.Height;

			if (_skills != null)
			{
				_skills.Height = height - _skills.Top;
				_skills.Size = new Size(splitSkills.Panel1.Width - 265, splitSkills.Panel1.Height - 39);

			}
			if (_groups != null)
			{
				_groups.Height = height - _groups.Top;
				_groups.Size = new Size(255, splitSkills.Panel1.Height - 15);
			}
		}

		private void Panel2_Resize(object sender, EventArgs e)
		{
			if (_knoSkills != null)
			{
				_knoSkills.Size = new Size(splitSkills.Panel2.Width - 6, splitSkills.Panel2.Height - 50);
				_knoSkills.Height = splitSkills.Panel2.Height - 53;
			}
		}

		private void cboDisplayFilter_SelectedIndexChanged(object sender, EventArgs e)
		{
			ComboBox csender = (ComboBox) sender;
			Tuple<string, Func<Skill, bool>> selectedItem = (Tuple<string, Func<Skill, bool>>)csender.SelectedItem;

			_skills.Filter(selectedItem.Item2);
		}

		private void btnExotic_Click(object sender, EventArgs e)
		{
			XmlDocument document = XmlManager.Instance.Load("skills.xml");
			frmSelectExoticSkill frmPickExoticSkill = new frmSelectExoticSkill();
			frmPickExoticSkill.ShowDialog(this);

			if (frmPickExoticSkill.DialogResult == DialogResult.Cancel)
				return;

			XmlNode node =
				document.SelectSingleNode("/chummer/skills/skill[name = \"" + frmPickExoticSkill.SelectedExoticSkill + "\"]");
			ObjCharacter.Skills.Add(new ExoticSkill(ObjCharacter, node));
		}

		private void btnKnowledge_Click(object sender, EventArgs e)
		{
			ObjCharacter.KnowledgeSkills.Add(new KnowledgeSkill(ObjCharacter));
		}

		
	}
}