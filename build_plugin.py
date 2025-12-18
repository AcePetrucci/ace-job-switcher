#!/usr/bin/env python3
"""
Script to build and package the AceJobSwitcher plugin.

This script:
1. Adds DownloadLink entries from the main AceJobSwitcher.json to the debug version
2. Wraps the debug JSON file content into an array
3. Creates a zip file of all contents in the bin/x64/Debug/ directory
"""

import json
import os
import shutil
import zipfile
from pathlib import Path


def main():
    # Define paths
    main_json_path = Path("AceJobSwitcher/AceJobSwitcher.json")
    debug_json_path = Path("AceJobSwitcher/bin/x64/Debug/AceJobSwitcher.json")
    debug_dir = Path("AceJobSwitcher/bin/x64/Debug")
    zip_path = debug_dir / "AceJobSwitcher.zip"
    
    print("Starting AceJobSwitcher build process...")
    
    # Step 1: Read the main JSON file to get DownloadLink entries
    print("1. Reading main AceJobSwitcher.json...")
    try:
        with open(main_json_path, 'r', encoding='utf-8') as f:
            main_json = json.load(f)
    except FileNotFoundError:
        print(f"Error: Could not find {main_json_path}")
        return False
    except json.JSONDecodeError as e:
        print(f"Error: Invalid JSON in {main_json_path}: {e}")
        return False
    
    # Step 2: Read the debug JSON file
    print("2. Reading debug AceJobSwitcher.json...")
    try:
        with open(debug_json_path, 'r', encoding='utf-8') as f:
            debug_json = json.load(f)
    except FileNotFoundError:
        print(f"Error: Could not find {debug_json_path}")
        return False
    except json.JSONDecodeError as e:
        print(f"Error: Invalid JSON in {debug_json_path}: {e}")
        return False
    
    # Step 3: Handle JSON format (object or array) and add DownloadLink entries
    print("3. Processing debug JSON format...")
    
    # Check if debug_json is already an array or a single object
    if isinstance(debug_json, list):
        print("   Debug JSON is already in array format")
        if len(debug_json) > 0 and isinstance(debug_json[0], dict):
            target_json = debug_json[0]  # Work with the first object in the array
        else:
            print("Error: Array format is unexpected - no valid object found")
            return False
    elif isinstance(debug_json, dict):
        print("   Debug JSON is in object format")
        target_json = debug_json
    else:
        print("Error: Debug JSON format is not recognized")
        return False
    
    # Step 4: Add DownloadLink entries
    print("4. Adding DownloadLink entries...")
    download_links = {
        "DownloadLinkInstall": main_json.get("DownloadLinkInstall"),
        "DownloadLinkTesting": main_json.get("DownloadLinkTesting"),
        "DownloadLinkUpdate": main_json.get("DownloadLinkUpdate")
    }
    
    # Add the download links to target JSON object
    for key, value in download_links.items():
        if value is not None:
            target_json[key] = value
            print(f"   Added {key}: {value}")
    
    # Step 5: Ensure JSON content is in array format
    print("5. Ensuring JSON content is in array format...")
    if isinstance(debug_json, list):
        json_array = debug_json  # Already an array, use as-is
    else:
        json_array = [target_json]  # Wrap single object in array
    
    # Step 6: Write the updated JSON back to file
    print("6. Writing updated JSON file...")
    try:
        with open(debug_json_path, 'w', encoding='utf-8') as f:
            json.dump(json_array, f, indent=2, ensure_ascii=False)
        print(f"   Successfully updated {debug_json_path}")
    except Exception as e:
        print(f"Error: Could not write to {debug_json_path}: {e}")
        return False
    
    # Step 7: Create zip file
    print("7. Creating zip file...")
    
    # Create zip file in a temporary location first to avoid corruption
    temp_zip_path = Path("AceJobSwitcher_temp.zip")
    
    # Remove existing zip files if they exist
    if zip_path.exists():
        zip_path.unlink()
        print(f"   Removed existing {zip_path.name}")
    if temp_zip_path.exists():
        temp_zip_path.unlink()
    
    try:
        with zipfile.ZipFile(temp_zip_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as zipf:
            # Walk through all files and directories in debug_dir
            for root, dirs, files in os.walk(debug_dir):
                root_path = Path(root)
                
                for file in files:
                    file_path = root_path / file
                    # Skip any existing zip files to avoid corruption
                    if file_path.suffix.lower() == '.zip':
                        continue
                    
                    # Calculate relative path from debug_dir
                    relative_path = file_path.relative_to(debug_dir)
                    zipf.write(file_path, relative_path)
                    print(f"   Added: {relative_path}")
        
        # Move the temporary zip file to the final location
        shutil.move(temp_zip_path, zip_path)
        print(f"   Successfully created {zip_path}")
        print(f"   Zip file size: {zip_path.stat().st_size / 1024:.1f} KB")
        
    except Exception as e:
        print(f"Error: Could not create zip file: {e}")
        return False
    
    print("\n✅ Build process completed successfully!")
    return True


if __name__ == "__main__":
    success = main()
    if not success:
        exit(1) 