# The workspace

The KillerBench window has three panes that follow the way you work: pick a section on the left, set the tool's options in the center, and read the output on the right. An activity log can open under the options.

## Sections

The sidebar on the left lists every tool in twelve sections: Diagnostics, Repair, Updates, Devices, Network, Apps, Maintenance, Onboarding, Security, BitLocker, Privacy and Storage. Drag its edge to resize it, or drag it past its minimum width to snap it closed.

## Tool options

The center pane shows the selected section, its tool list and the options for the chosen tool. Options that could change the machine are always off by default. Press **Run diagnosis** to start the tool. When **Auto-repair confirmed issues** is turned on, the same button reads **Run repair**. **Stop** ends the run after the current command finishes.

## Output

The output pane on the right has two views.

- **Raw output** shows the technical evidence as it is collected, with original line numbers. It opens on its own while a tool runs.
- **Readable report** shows the finished report for the last run. It replaces the raw output when the run finishes.

Use the filter box in Raw output to show only matching lines. **Export report** saves the report with its source paths and evidence excerpts. **Session sources** lists every piece of evidence gathered in this session.

## Activity log

The activity log under the tool options records what KillerBench did in this session.

## Themes, languages and shortcuts

The rail between the sidebar and the tool options holds the theme picker, the language picker, the shortcut list and the activity log toggle. KillerBench has 13 themes and 19 languages.

The smoked-glass console and readable reports follow the active theme. Confirmations and Open, Save, and Folder dialogs use the same family styling, including the 98SE theme. Choose the theme before opening a dialog to use that appearance throughout the workflow.

| Key | Action |
| --- | --- |
| F1 | Keyboard shortcuts |
| F2 | Show or hide the sidebar |
| F3 | Show or hide the activity log |
| F12 | About KillerBench |
| Ctrl+O | Open evidence |
| Esc | Close an overlay or stop reading a file |
