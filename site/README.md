# The Last Light field guide

Static showcase, compatible with GitHub Pages. No build step, keys, accounts or remote services required at runtime. All model-viewer JS, models, images and video are local.

Preview from the Unity project root:

    python3 -m http.server 8091 --directory site

Open http://localhost:8091. Use HTTP for the 3D models; browsers may block GLB loading from file://.

Refresh script snapshots:

    python3 Tools/site/export_data.py

Rebuild the actual map (Unity must be closed):

    LL_EXPORT_SITE_MAP=1 /Applications/Unity/Unity-6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -burst-disable-compilation -projectPath "$PWD" -runTests -testPlatform PlayMode -testFilter WebsiteMapExportTest -testResults /tmp/site-map-test.xml -logFile /tmp/site-map.log
    /Applications/Blender.app/Contents/MacOS/Blender -b --python Tools/site/build_runtime_map.py

The export reads runtime MeshFilters without modifying the scene. Intermediate JSON is written to Library/Website. The GLB includes the actual nine-room layout and props, with simplified materials, no ceiling and a 1.7 m cutaway. The old Anatomy figure is no longer loaded.

The current core script relationships are curated in export_data.py, and should be reviewed when gameplay changes. The source defaults are extracted; runtime overrides are documented separately. This is not yet the CGDD toolkit export pipeline.

Web models can be regenerated with Blender running Tools/site/prepare_models.py. The source GLBs stay unchanged. Media/play.mp4 is the October 19 recording, preceding the October 20 character replacements. Reflection drafts are browser-local and can be exported as Markdown.

Deployment: https://cjameeely.github.io/back_rooms/

Repository: https://github.com/CJameeely/back_rooms

Pages publishes site/ through .github/workflows/pages.yml. Deployment checkout: ../deploy/backrooms-pages relative to the local Unity project root. Push site changes to main to trigger deployment. The repository is public. Existing CGDD export files are retained.
