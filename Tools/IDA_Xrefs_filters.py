import re
import sys
import os

# Configuration
# Add your regex patterns here
BLACKLIST_PATTERNS = [
    r"^System\..*",
    r"^UnityEngine\..*",
    r"^TMPro\..*",
    r"^MS\..*",
    r"^sub_.*",
    # Add more patterns as needed
]

try:
    import graphviz
    HAS_GRAPHVIZ = True
except ImportError:
    HAS_GRAPHVIZ = False

def parse_gdl(content):
    """
    Parses GDL content into nodes, edges, and raw lines.
    Returns:
        nodes: dict {id: {attrs}}
        edges: list of {attrs}
        header_lines: list of strings (lines before the first node/edge)
        footer_lines: list of strings (lines after the graph content)
    """
    nodes = {}
    edges = []
    lines = content.splitlines()
    
    # Regex for parsing
    node_re = re.compile(r'^\s*node:\s*\{(.*)\}\s*$')
    edge_re = re.compile(r'^\s*edge:\s*\{(.*)\}\s*$')
    attr_re = re.compile(r'(\w+):\s*("[^"]*"|[^\s\}]+)')
    
    # Simple state machine
    in_graph = False
    
    processed_lines = []
    
    for line in lines:
        line_stripped = line.strip()
        
        # Check for node
        node_match = node_re.match(line_stripped)
        if node_match:
            attrs_str = node_match.group(1)
            attrs = dict(attr_re.findall(attrs_str))
            # Clean up quotes
            for k, v in attrs.items():
                if v.startswith('"') and v.endswith('"'):
                    attrs[k] = v[1:-1]
            
            if 'title' in attrs:
                nodes[attrs['title']] = {'attrs': attrs, 'raw': line}
            continue

        # Check for edge
        edge_match = edge_re.match(line_stripped)
        if edge_match:
            attrs_str = edge_match.group(1)
            attrs = dict(attr_re.findall(attrs_str))
            # Clean up quotes
            for k, v in attrs.items():
                if v.startswith('"') and v.endswith('"'):
                    attrs[k] = v[1:-1]
            
            edges.append({'attrs': attrs, 'raw': line})
            continue
            
        # Store other lines (headers, footers, comments)
        # We might need to reconstruct the file, but for now we just store them?
        # Actually, to reconstruct the file exactly, we need to know where nodes/edges were.
        # But since we are filtering, we can just dump headers, then nodes, then edges, then footer.
        processed_lines.append(line)

    return nodes, edges, processed_lines

def filter_graph(nodes, edges, blacklist_patterns):
    """
    Filters nodes and edges based on blacklist patterns.
    Implements recursive deletion for isolated subgraphs.
    """
    compiled_patterns = [re.compile(p) for p in blacklist_patterns]
    
    nodes_to_delete = set()
    
    # 1. Identify nodes matching the blacklist
    for node_id, node_data in nodes.items():
        label = node_data['attrs'].get('label', '')
        for pattern in compiled_patterns:
            if pattern.match(label):
                nodes_to_delete.add(node_id)
                break
    
    print(f"Found {len(nodes_to_delete)} nodes matching blacklist patterns.")
    
    # 2. Recursive deletion (Orphan removal)
    # If a node's parents are all deleted, it should be deleted too (unless it's a root)
    # Build adjacency for reverse lookup (target -> sources)
    target_to_sources = {}
    for edge in edges:
        src = edge['attrs'].get('sourcename')
        dst = edge['attrs'].get('targetname')
        if src and dst:
            if dst not in target_to_sources:
                target_to_sources[dst] = set()
            target_to_sources[dst].add(src)
            
    # Iteratively propagate deletion
    changed = True
    while changed:
        changed = False
        # Check all nodes not yet marked for deletion
        for node_id in nodes:
            if node_id in nodes_to_delete:
                continue
                
            # Get parents
            parents = target_to_sources.get(node_id, set())
            
            # If it has parents, and ALL parents are marked for deletion, delete it.
            # If it has NO parents, it's a root, so we keep it (unless it matched blacklist directly)
            if len(parents) > 0:
                all_parents_deleted = all(p in nodes_to_delete for p in parents)
                if all_parents_deleted:
                    nodes_to_delete.add(node_id)
                    changed = True
                    # print(f"Recursively deleting node {node_id} ({nodes[node_id]['attrs'].get('label')})")

    print(f"Total nodes to delete after recursion: {len(nodes_to_delete)}")
    
    # 3. Filter
    final_nodes = {}
    for node_id, node_data in nodes.items():
        if node_id not in nodes_to_delete:
            final_nodes[node_id] = node_data
            
    final_edges = []
    for edge in edges:
        src = edge['attrs'].get('sourcename')
        dst = edge['attrs'].get('targetname')
        
        if src in nodes_to_delete or dst in nodes_to_delete:
            continue
        final_edges.append(edge)
        
    return final_nodes, final_edges

def export_to_dot(nodes, edges, output_path):
    """
    Exports the graph to Graphviz DOT format.
    """
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write("digraph G {\n")
        f.write("  rankdir=LR;\n")
        f.write("  node [shape=box, style=filled, fontname=\"Arial\"];\n")
        
        # Write nodes
        for node_id, node_data in nodes.items():
            label = node_data['attrs'].get('label', node_id)
            # Escape quotes in label
            label = label.replace('"', '\\"')
            
            # Try to preserve colors if possible (GDL colors are usually palette indices, hard to map without palette)
            # For now, just use default or simple mapping if needed.
            # We'll just output label.
            f.write(f"  \"{node_id}\" [label=\"{label}\"];\n")
            
        # Write edges
        for edge in edges:
            src = edge['attrs'].get('sourcename')
            dst = edge['attrs'].get('targetname')
            if src and dst:
                f.write(f"  \"{src}\" -> \"{dst}\";\n")
                
        f.write("}\n")

def export_to_svg(nodes, edges, output_path):
    """
    Exports the graph to SVG using graphviz library.
    """
    if not HAS_GRAPHVIZ:
        print("Error: 'graphviz' python library is not installed. Cannot export to SVG directly.")
        print("Please install it using: pip install graphviz")
        print("Or ensure Graphviz executables are in your PATH.")
        return

    dot = graphviz.Digraph(comment='Filtered Graph')
    dot.attr(rankdir='LR')
    dot.attr('node', shape='box', style='filled', fontname='Arial')
    
    # Add nodes
    for node_id, node_data in nodes.items():
        label = node_data['attrs'].get('label', node_id)
        dot.node(node_id, label=label)
        
    # Add edges
    for edge in edges:
        src = edge['attrs'].get('sourcename')
        dst = edge['attrs'].get('targetname')
        if src and dst:
            dot.edge(src, dst)
            
    try:
        # render() saves the source file and then renders it. 
        # format='svg' will produce output_path.svg (if we pass filename without extension)
        # But we want to control the exact output filename.
        
        # If output_path ends with .svg, strip it for the filename argument, 
        # because render appends the format.
        base_name = output_path
        if base_name.lower().endswith('.svg'):
            base_name = base_name[:-4]
            
        output_file = dot.render(filename=base_name, format='svg', cleanup=True)
        # Rename if necessary (dot.render might append .svg)
        # Actually dot.render(filename='foo', format='svg') creates 'foo.svg'.
        # If user passed 'foo.svg', base_name is 'foo', result is 'foo.svg'. Correct.
        
        print(f"SVG exported to: {output_file}")
        
    except Exception as e:
        print(f"Error exporting SVG: {e}")
        print("Make sure Graphviz executables (dot) are installed and in your PATH.")

def export_to_html(nodes, edges, output_path):
    """
    Exports the graph to a self-contained HTML file using Vis.js.
    """
    # Prepare data for Vis.js
    vis_nodes = []
    for node_id, node_data in nodes.items():
        label = node_data['attrs'].get('label', node_id)
        # Simple escaping
        label = label.replace("'", "\\'")
        vis_nodes.append(f"{{id: '{node_id}', label: '{label}', shape: 'box'}}")
        
    vis_edges = []
    for edge in edges:
        src = edge['attrs'].get('sourcename')
        dst = edge['attrs'].get('targetname')
        if src and dst:
            vis_edges.append(f"{{from: '{src}', to: '{dst}', arrows: 'to'}}")
            
    html_content = f"""<!DOCTYPE html>
<html>
<head>
    <title>Filtered Graph</title>
    <script type="text/javascript" src="https://unpkg.com/vis-network/standalone/umd/vis-network.min.js"></script>
    <style type="text/css">
        body {{ font-family: sans-serif; margin: 0; padding: 0; }}
        #controls {{
            padding: 10px;
            background: #f5f5f5;
            border-bottom: 1px solid #ddd;
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 40px;
            z-index: 10;
            display: flex;
            align-items: center;
        }}
        #mynetwork {{
            width: 100%;
            height: calc(100vh - 60px);
            margin-top: 60px;
            border: none;
        }}
    </style>
</head>
<body>
<div id="controls">
    <label style="margin-right: 20px;">
        <input type="checkbox" id="layoutToggle" checked> 
        <strong>Lock Layout (Hierarchical)</strong>
    </label>
    <span style="color: #666; font-size: 0.9em;">Uncheck to enable free movement of nodes.</span>
</div>
<div id="mynetwork"></div>
<script type="text/javascript">
    // create an array with nodes
    var nodes = new vis.DataSet([
        {",".join(vis_nodes)}
    ]);

    // create an array with edges
    var edges = new vis.DataSet([
        {",".join(vis_edges)}
    ]);

    // create a network
    var container = document.getElementById('mynetwork');
    var data = {{
        nodes: nodes,
        edges: edges
    }};
    
    var options = {{
        layout: {{
            hierarchical: {{
                enabled: true,
                direction: "LR",
                sortMethod: "directed",
                levelSeparation: 250,
                nodeSpacing: 100
            }}
        }},
        physics: {{
            enabled: false
        }},
        interaction: {{
            navigationButtons: true,
            keyboard: true
        }}
    }};
    
    var network = new vis.Network(container, data, options);
    
    // Toggle layout mode
    document.getElementById('layoutToggle').addEventListener('change', function(e) {{
        var isLocked = e.target.checked;
        network.setOptions({{
            layout: {{
                hierarchical: {{
                    enabled: isLocked
                }}
            }},
            physics: {{
                // When unlocked, we disable physics so nodes stay where they are but can be moved.
                // If you want auto-layout, set enabled: true here.
                enabled: false 
            }}
        }});
    }});
</script>
</body>
</html>
"""
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(html_content)
    print(f"HTML exported to: {output_path}")

def write_gdl(output_path, header_lines, nodes, edges):
    with open(output_path, 'w', encoding='utf-8') as f:
        # Write headers (everything that wasn't a node or edge)
        # Note: This is a simplification. The original file might have interleaved comments.
        # But GDL structure is usually Header -> Nodes -> Edges -> Footer.
        # We will try to preserve the 'graph: {' line and others.
        
        # Find where the graph starts
        graph_start_index = -1
        for i, line in enumerate(header_lines):
            if "graph:" in line:
                graph_start_index = i
                break
        
        if graph_start_index != -1:
            # Write everything up to graph start + 1 (assuming '{' is on same line or next)
            # Actually, let's just dump the header lines, but we need to be careful about the closing brace '}'
            # The parser put all non-node/edge lines in header_lines.
            # This includes the closing '}'.
            pass
            
        # Heuristic: Write all lines that appear before any node/edge in the original file?
        # Since we lost the order, let's just reconstruct a valid GDL.
        
        # Write header lines until we see the first closing brace or end
        # This is tricky. Let's just write standard GDL structure.
        
        f.write("graph: {\n")
        f.write("title: \"Filtered Graph\"\n")
        
        # Write nodes
        for node in nodes.values():
            f.write(node['raw'] + "\n")
            
        # Write edges
        for edge in edges:
            f.write(edge['raw'] + "\n")
            
        f.write("}\n")

def main():
    input_file = "graph.gdl"
    output_file = "graph_filtered.gdl"
    
    if len(sys.argv) > 1:
        input_file = sys.argv[1]
    if len(sys.argv) > 2:
        output_file = sys.argv[2]
        
    if not os.path.exists(input_file):
        print(f"Error: Input file '{input_file}' not found.")
        return

    print(f"Reading {input_file}...")
    with open(input_file, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
        
    nodes, edges, misc_lines = parse_gdl(content)
    print(f"Parsed {len(nodes)} nodes and {len(edges)} edges.")
    
    final_nodes, final_edges = filter_graph(nodes, edges, BLACKLIST_PATTERNS)
    
    print(f"Remaining: {len(final_nodes)} nodes and {len(final_edges)} edges.")
    
    if output_file.lower().endswith('.svg'):
        print("Exporting to SVG...")
        export_to_svg(final_nodes, final_edges, output_file)
    elif output_file.lower().endswith('.dot'):
        print("Exporting to DOT...")
        export_to_dot(final_nodes, final_edges, output_file)
    elif output_file.lower().endswith('.html'):
        print("Exporting to HTML...")
        export_to_html(final_nodes, final_edges, output_file)
    else:
        # Reconstruct GDL file
        # We use a simple writer that ignores the original header/footer complexity for now
        # but tries to keep it valid GDL.
        # To preserve colors etc from original, we should look at misc_lines
        
        with open(output_file, 'w', encoding='utf-8') as f:
            # Try to find the opening graph line in misc_lines
            written_header = False
            for line in misc_lines:
                if line.strip() == "}":
                    continue # Skip closing brace, we'll add it at end
                f.write(line + "\n")
                
            # Write nodes
            for node in final_nodes.values():
                f.write(node['raw'] + "\n")
                
            # Write edges
            for edge in final_edges:
                f.write(edge['raw'] + "\n")
                
            f.write("}\n")
            
        print(f"Written to {output_file}")

if __name__ == "__main__":
    main()
