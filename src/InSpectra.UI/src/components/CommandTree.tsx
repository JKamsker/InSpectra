import { ChevronDown, ChevronRight, TerminalSquare } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { NormalizedCommand } from "../data/normalize";

interface CommandTreeProps {
  commands: NormalizedCommand[];
  searchTerm: string;
  selectedPath?: string;
  onSelect: (path: string) => void;
}

export function CommandTree({ commands, searchTerm, selectedPath, onSelect }: CommandTreeProps) {
  if (commands.length === 0) {
    return <p className="sidebar-empty">No commands are available in this snapshot.</p>;
  }

  return (
    <div className="command-tree">
      {commands.map((command) => (
        <TreeNode
          key={command.path}
          command={command}
          searchTerm={searchTerm}
          selectedPath={selectedPath}
          onSelect={onSelect}
        />
      ))}
    </div>
  );
}

function TreeNode({
  command,
  searchTerm,
  selectedPath,
  onSelect,
}: {
  command: NormalizedCommand;
  searchTerm: string;
  selectedPath?: string;
  onSelect: (path: string) => void;
}) {
  const [manualExpanded, setManualExpanded] = useState(false);
  const [autoCollapsed, setAutoCollapsed] = useState(false);
  const prevSelectedPath = useRef(selectedPath);

  useEffect(() => {
    if (prevSelectedPath.current !== selectedPath) {
      setAutoCollapsed(false);
      prevSelectedPath.current = selectedPath;
    }
  }, [selectedPath]);

  const normalizedSearch = searchTerm.trim().toLowerCase();
  const matches = matchesCommand(command, normalizedSearch);

  if (!matches) {
    return null;
  }

  const hasChildren = command.commands.length > 0;
  const isSelected = selectedPath === command.path;
  const isAncestorOfSelected = hasChildren && selectedPath !== undefined &&
    (isSelected || selectedPath.startsWith(command.path + " "));
  const isExpanded = normalizedSearch.length > 0 || manualExpanded || (isAncestorOfSelected && !autoCollapsed);

  return (
    <div className="tree-node">
      <button
        type="button"
        className={`tree-row ${isSelected ? "selected" : ""}`}
        onClick={() => {
          if (isSelected && hasChildren) {
            setAutoCollapsed((value) => !value);
          } else {
            onSelect(command.path);
          }
        }}
      >
        <span
          className="tree-toggle"
          onClick={(event) => {
            if (!hasChildren) {
              return;
            }

            event.stopPropagation();
            if (isAncestorOfSelected && !manualExpanded) {
              setAutoCollapsed((value) => !value);
            } else {
              setManualExpanded((value) => !value);
            }
          }}
        >
          {hasChildren ? (
            isExpanded ? <ChevronDown aria-hidden="true" /> : <ChevronRight aria-hidden="true" />
          ) : (
            <TerminalSquare aria-hidden="true" />
          )}
        </span>
        <span className="tree-copy">
          <strong>{command.command.name}</strong>
        </span>
      </button>

      {hasChildren && isExpanded ? (
        <div className="tree-children">
          {command.commands.map((child) => (
            <TreeNode
              key={child.path}
              command={child}
              searchTerm={searchTerm}
              selectedPath={selectedPath}
              onSelect={onSelect}
            />
          ))}
        </div>
      ) : null}
    </div>
  );
}

function matchesCommand(command: NormalizedCommand, searchTerm: string): boolean {
  if (searchTerm.length === 0) {
    return true;
  }

  const haystacks = [command.path, command.command.name, command.command.description ?? ""]
    .join(" ")
    .toLowerCase();

  if (haystacks.includes(searchTerm)) {
    return true;
  }

  return command.commands.some((child) => matchesCommand(child, searchTerm));
}
